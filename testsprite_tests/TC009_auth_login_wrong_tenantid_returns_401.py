import requests

def test_auth_login_wrong_tenantid_returns_401():
    url = "http://localhost:80/v1/auth/login"
    payload = {
        "email": "prdtest@test.pe",
        "password": "PrdTest2026!",
        "tenantId": "00000000-0000-0000-0000-000000000000"
    }
    headers = {
        "Content-Type": "application/json"
    }
    try:
        response = requests.post(url, json=payload, headers=headers, timeout=30)
    except requests.RequestException as e:
        assert False, f"Request failed: {e}"

    # Expect HTTP 401 Unauthorized
    assert response.status_code == 401, f"Expected status 401 but got {response.status_code}"

    try:
        resp_json = response.json()
    except ValueError:
        resp_json = None

    # The response JSON should contain an error field
    assert resp_json is not None and isinstance(resp_json, dict), "Response is not valid JSON"
    assert 'error' in resp_json or 'message' in resp_json, "Response JSON must contain 'error' or 'message' field indicating unauthorized"


test_auth_login_wrong_tenantid_returns_401()