import requests

def test_auth_login_wrong_password_returns_401():
    base_url = "http://localhost:80"
    endpoint = "/v1/auth/login"
    url = base_url + endpoint
    payload = {
        "email": "prdtest@test.pe",
        "password": "WRONG_PASSWORD_123",
        "tenantId": "b2ba4cad-45c9-4a98-96f9-bf2eb6c96572"
    }
    headers = {
        "Content-Type": "application/json"
    }
    try:
        response = requests.post(url, json=payload, headers=headers, timeout=30)
    except requests.RequestException as e:
        assert False, f"Request failed: {e}"

    assert response.status_code == 401, f"Expected status 401, got {response.status_code}"
    try:
        json_body = response.json()
    except ValueError:
        assert False, "Response is not valid JSON"

    assert isinstance(json_body, dict), "Response JSON is not an object"
    assert "error" in json_body, "Response JSON does not contain 'error' field"

test_auth_login_wrong_password_returns_401()