import requests

BASE_URL = "http://localhost:80"
LOGIN_URL = f"{BASE_URL}/v1/auth/login"
PERCEPTIONS_URL = f"{BASE_URL}/v1/perceptions"
LOGIN_PAYLOAD = {
    "email": "prdtest@test.pe",
    "password": "PrdTest2026!",
    "tenantId": "b2ba4cad-45c9-4a98-96f9-bf2eb6c96572"
}
TIMEOUT = 30

def test_perception_list():
    # Step 1: Authenticate and get access token
    try:
        login_resp = requests.post(LOGIN_URL, json=LOGIN_PAYLOAD, timeout=TIMEOUT)
        assert login_resp.status_code == 200, f"Login failed with status {login_resp.status_code}"
        login_json = login_resp.json()
        assert 'accessToken' in login_json and isinstance(login_json['accessToken'], str) and login_json['accessToken'].startswith('eyJ'), "accessToken missing or malformed in login response"
        access_token = login_json['accessToken']
    except (requests.RequestException, AssertionError) as e:
        raise AssertionError(f"Authentication failed: {str(e)}")

    headers = {
        "Authorization": f"Bearer {access_token}"
    }

    # Step 2: GET /v1/perceptions?page=1&pageSize=10
    try:
        params = {
            "page": 1,
            "pageSize": 10
        }
        resp = requests.get(PERCEPTIONS_URL, headers=headers, params=params, timeout=TIMEOUT)
        assert resp.status_code == 200, f"Perceptions list request failed with status {resp.status_code}"
        json_data = resp.json()
        # Validate required fields 'items' as array with at least 1 item and 'totalCount'
        assert 'items' in json_data and isinstance(json_data['items'], list), "'items' missing or not a list in response"
        assert len(json_data['items']) >= 1, "'items' array is empty, expected at least 1 item"
        assert 'totalCount' in json_data and isinstance(json_data['totalCount'], int), "'totalCount' missing or not an int in response"
    except (requests.RequestException, AssertionError) as e:
        raise AssertionError(f"Perception list test failed: {str(e)}")

test_perception_list()