import requests

BASE_URL = "http://localhost:80"
LOGIN_URL = f"{BASE_URL}/v1/auth/login"
TENANT_URL = f"{BASE_URL}/v1/tenant"
LOGIN_PAYLOAD = {
    "email": "prdtest@test.pe",
    "password": "PrdTest2026!",
    "tenantId": "b2ba4cad-45c9-4a98-96f9-bf2eb6c96572"
}
TIMEOUT = 30

def test_tenant_get_and_update():
    # Step 1: Authenticate and get access token
    try:
        login_response = requests.post(LOGIN_URL, json=LOGIN_PAYLOAD, timeout=TIMEOUT)
        assert login_response.status_code == 200, f"Login failed with status {login_response.status_code}"
        login_json = login_response.json()
        access_token = login_json.get("accessToken")
        assert access_token and access_token.startswith("eyJ"), "Invalid accessToken"
    except (requests.RequestException, AssertionError) as e:
        raise AssertionError(f"Login request failed or assertions error: {e}")

    headers = {
        "Authorization": f"Bearer {access_token}",
        "Content-Type": "application/json"
    }

    # Step 2: GET /v1/tenant and verify 'ruc' present
    try:
        get_response_1 = requests.get(TENANT_URL, headers=headers, timeout=TIMEOUT)
        assert get_response_1.status_code == 200, f"GET /v1/tenant failed with status {get_response_1.status_code}"
        tenant_data = get_response_1.json()
        assert "ruc" in tenant_data and tenant_data["ruc"], "'ruc' field missing or empty in tenant data"
    except (requests.RequestException, AssertionError) as e:
        raise AssertionError(f"Initial GET /v1/tenant failed or assertions error: {e}")

    # Step 3: PUT /v1/tenant with update body
    update_payload = {
        "nombreComercial": "PRD Updated",
        "direccion": "Av PRD 2026"
    }
    try:
        put_response = requests.put(TENANT_URL, headers=headers, json=update_payload, timeout=TIMEOUT)
        assert put_response.status_code == 204, f"PUT /v1/tenant failed with status {put_response.status_code}"
        # No content expected
    except (requests.RequestException, AssertionError) as e:
        raise AssertionError(f"PUT /v1/tenant failed or assertions error: {e}")

    # Step 4: GET /v1/tenant again to verify updated values
    try:
        get_response_2 = requests.get(TENANT_URL, headers=headers, timeout=TIMEOUT)
        assert get_response_2.status_code == 200, f"Second GET /v1/tenant failed with status {get_response_2.status_code}"
        tenant_updated_data = get_response_2.json()
        # Verify the updated fields
        assert tenant_updated_data.get("nombreComercial") == update_payload["nombreComercial"], \
            "nombreComercial was not updated correctly"
        assert tenant_updated_data.get("direccion") == update_payload["direccion"], \
            "direccion was not updated correctly"
        # Also verify ruc still present
        assert "ruc" in tenant_updated_data and tenant_updated_data["ruc"], "'ruc' missing or empty after update"
    except (requests.RequestException, AssertionError) as e:
        raise AssertionError(f"Verification GET /v1/tenant failed or assertions error: {e}")

test_tenant_get_and_update()
