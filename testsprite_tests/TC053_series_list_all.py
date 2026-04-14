import requests

def test_TC053_series_list_all():
    base_url = "http://localhost:80"
    login_url = f"{base_url}/v1/auth/login"
    series_url = f"{base_url}/v1/series"
    login_payload = {
        "email": "prdtest@test.pe",
        "password": "PrdTest2026!",
        "tenantId": "b2ba4cad-45c9-4a98-96f9-bf2eb6c96572"
    }
    timeout = 30

    try:
        # Step 1: Authenticate to obtain access token
        login_resp = requests.post(login_url, json=login_payload, timeout=timeout)
        assert login_resp.status_code == 200, f"Login failed with status {login_resp.status_code}"
        login_json = login_resp.json()
        access_token = login_json.get("accessToken")
        assert access_token and isinstance(access_token, str) and access_token.startswith("eyJ"), "Invalid accessToken"

        headers = {
            "Authorization": f"Bearer {access_token}"
        }

        # Step 2: GET /v1/series with Bearer token
        series_resp = requests.get(series_url, headers=headers, timeout=timeout)
        assert series_resp.status_code == 200, f"/v1/series GET failed with status {series_resp.status_code}"
        data = series_resp.json()
        assert isinstance(data, list), "Response is not a JSON array"

        # Validate each element has required fields
        required_fields = {'id', 'documentType', 'serie', 'currentCorrelative', 'emissionPoint', 'isActive', 'createdAt'}
        for item in data:
            assert isinstance(item, dict), "Series item is not an object"
            missing = required_fields - item.keys()
            assert not missing, f"Series item missing fields: {missing}"

        # Validate array has at least 1 item
        assert len(data) >= 1, f"Series list is empty"

    except requests.exceptions.RequestException as e:
        assert False, f"RequestException occurred: {e}"

test_TC053_series_list_all()