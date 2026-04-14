import requests

BASE_URL = "http://localhost:80"
BACKOFFICE_LOGIN_ENDPOINT = "/v1/backoffice/auth/login"
DASHBOARD_ENDPOINT = "/v1/backoffice/dashboard"

BACKOFFICE_EMAIL = "superadmin@tukifact.net.pe"
BACKOFFICE_PASSWORD = "SuperAdmin2026!"

def test_backoffice_dashboard_global_metrics():
    session = requests.Session()
    try:
        # Authenticate to get backoffice access token
        login_resp = session.post(
            BASE_URL + BACKOFFICE_LOGIN_ENDPOINT,
            json={
                "email": BACKOFFICE_EMAIL,
                "password": BACKOFFICE_PASSWORD
            },
            timeout=30
        )
        assert login_resp.status_code == 200, f"Backoffice login failed: {login_resp.status_code} {login_resp.text}"
        login_json = login_resp.json()
        assert "accessToken" in login_json, "accessToken missing in login response"
        access_token = login_json["accessToken"]
        assert isinstance(access_token, str) and access_token.startswith("eyJ"), "Invalid accessToken format"
        user = login_json.get("user")
        assert user is not None and user.get("role") == "superadmin", "User role is not superadmin"

        headers = {
            "Authorization": f"Bearer {access_token}"
        }

        # Call the backoffice dashboard endpoint
        dashboard_resp = session.get(
            BASE_URL + DASHBOARD_ENDPOINT,
            headers=headers,
            timeout=30
        )
        assert dashboard_resp.status_code == 200, f"Dashboard request failed: {dashboard_resp.status_code} {dashboard_resp.text}"
        data = dashboard_resp.json()

        # Validate required fields exist and have correct types/values
        required_number_fields = [
            "totalTenants",
            "activeTenants",
            "suspendedTenants",
            "totalUsers",
            "totalDocuments",
            "todayDocuments",
            "monthDocuments"
        ]
        for field in required_number_fields:
            assert field in data, f"Missing field '{field}' in response"
            value = data[field]
            assert isinstance(value, (int, float)), f"Field '{field}' is not a number"
            if field == "totalTenants":
                assert value >= 1, f"Field '{field}' must be >= 1"
            else:
                assert value >= 0, f"Field '{field}' must be >= 0"

        # recentTenants must be an array with max length 5
        assert "recentTenants" in data, "Missing field 'recentTenants'"
        recent_tenants = data["recentTenants"]
        assert isinstance(recent_tenants, list), "recentTenants is not a list"
        assert len(recent_tenants) <= 5, f"recentTenants length is greater than 5: {len(recent_tenants)}"

        # tenantsByPlan must be an array
        assert "tenantsByPlan" in data, "Missing field 'tenantsByPlan'"
        tenants_by_plan = data["tenantsByPlan"]
        assert isinstance(tenants_by_plan, list), "tenantsByPlan is not a list"

    finally:
        session.close()

test_backoffice_dashboard_global_metrics()