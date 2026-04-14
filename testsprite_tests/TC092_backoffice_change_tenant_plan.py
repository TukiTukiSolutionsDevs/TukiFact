import requests

BASE_URL = "http://localhost:80"
BACKOFFICE_AUTH_ENDPOINT = "/v1/backoffice/auth/login"
PLANS_ENDPOINT = "/v1/plans"
CHANGE_TENANT_PLAN_ENDPOINT = "/v1/backoffice/tenants/{tenantId}/plan"

BACKOFFICE_CREDENTIALS = {
    "email": "superadmin@tukifact.net.pe",
    "password": "SuperAdmin2026!"
}

TENANT_ID = "b2ba4cad-45c9-4a98-96f9-bf2eb6c96572"
TIMEOUT = 30


def test_backoffice_change_tenant_plan():
    try:
        # Authenticate as backoffice user
        auth_resp = requests.post(
            BASE_URL + BACKOFFICE_AUTH_ENDPOINT,
            json=BACKOFFICE_CREDENTIALS,
            timeout=TIMEOUT
        )
        assert auth_resp.status_code == 200, f"Backoffice login failed: {auth_resp.text}"
        auth_json = auth_resp.json()
        access_token = auth_json.get("accessToken", "")
        assert access_token and access_token.startswith("eyJ"), "Invalid backoffice accessToken"
        assert auth_json.get("user", {}).get("role") == "superadmin", "User role is not superadmin"

        headers = {
            "Authorization": f"Bearer {access_token}",
            "Content-Type": "application/json"
        }

        # Get plans (public, no auth)
        plans_resp = requests.get(
            BASE_URL + PLANS_ENDPOINT,
            timeout=TIMEOUT
        )
        assert plans_resp.status_code == 200, f"GET /v1/plans failed: {plans_resp.text}"
        plans = plans_resp.json()
        assert isinstance(plans, list), "Plans response is not a list"
        assert len(plans) >= 2, "Not enough plans available to select the second plan"

        # Pick the second plan but not Free (assuming index 1)
        selected_plan = plans[1]
        assert "id" in selected_plan or "_id" in selected_plan or "planId" in selected_plan or "plan_id" in selected_plan, \
            "Selected plan does not have an ID field"
        # The naming of the plan id field might vary. Try common fields:
        plan_id = (
            selected_plan.get("id")
            or selected_plan.get("_id")
            or selected_plan.get("planId")
            or selected_plan.get("plan_id")
        )
        assert plan_id is not None, "Plan ID not found in selected plan"

        # Verify that selected plan is not free if there's a 'name' or 'priceMonthly' field.
        # If there is a 'name' field, ensure it's not 'Free' (case insensitive)
        name = selected_plan.get("name", "")
        if name.lower() == "free":
            # If second plan is Free, try to find a plan other than Free
            for plan in plans:
                if plan.get("name", "").lower() != "free":
                    plan_id = (
                        plan.get("id")
                        or plan.get("_id")
                        or plan.get("planId")
                        or plan.get("plan_id")
                    )
                    assert plan_id is not None, "Plan ID not found in fallback plan"
                    name = plan.get("name", "")
                    break
            else:
                assert False, "No plan other than Free found"

        # Change tenant plan using PUT /v1/backoffice/tenants/{tenantId}/plan
        change_plan_url = BASE_URL + CHANGE_TENANT_PLAN_ENDPOINT.format(tenantId=TENANT_ID)
        payload = {"planId": plan_id}

        change_resp = requests.put(
            change_plan_url,
            json=payload,
            headers=headers,
            timeout=TIMEOUT
        )
        assert change_resp.status_code == 200, f"PUT change plan failed: {change_resp.text}"
        change_json = change_resp.json()
        message = change_json.get("message", "")
        assert "Plan cambiado" in message, f"Unexpected message content: {message}"

    except (requests.RequestException, AssertionError) as e:
        raise AssertionError(f"Test TC092 failed: {e}")


test_backoffice_change_tenant_plan()