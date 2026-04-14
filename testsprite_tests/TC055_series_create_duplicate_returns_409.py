import requests

def test_series_create_duplicate_returns_409():
    base_url = "http://localhost:80"
    login_url = f"{base_url}/v1/auth/login"
    series_url = f"{base_url}/v1/series"
    auth_payload = {
        "email": "prdtest@test.pe",
        "password": "PrdTest2026!",
        "tenantId": "b2ba4cad-45c9-4a98-96f9-bf2eb6c96572"
    }

    try:
        # Step 1: Authenticate and get access token
        login_resp = requests.post(login_url, json=auth_payload, timeout=30)
        assert login_resp.status_code == 200, f"Login failed with status {login_resp.status_code}"
        login_json = login_resp.json()
        access_token = login_json.get("accessToken")
        assert access_token and access_token.startswith("eyJ"), "Invalid accessToken in login response"

        headers = {
            "Authorization": f"Bearer {access_token}",
            "Content-Type": "application/json"
        }

        # Step 2: Attempt to create duplicate series F001 which already exists
        duplicate_series_payload = {
            "documentType": "01",
            "serie": "F001",
            "emissionPoint": "0001"
        }
        series_resp = requests.post(series_url, json=duplicate_series_payload, headers=headers, timeout=30)

        # Step 3: Validate that response is 409 Conflict
        assert series_resp.status_code == 409, f"Expected 409 Conflict, got {series_resp.status_code}"

    except requests.RequestException as e:
        assert False, f"Request failed: {str(e)}"

test_series_create_duplicate_returns_409()