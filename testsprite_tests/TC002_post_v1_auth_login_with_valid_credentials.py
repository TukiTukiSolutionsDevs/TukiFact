import requests

def test_post_v1_auth_login_with_valid_credentials():
    base_url = "http://localhost:5186"
    url = f"{base_url}/v1/auth/login"
    payload = {
        "email": "admin@tukitest.pe",
        "password": "TestSprite2026!",
        "tenantId": "0c700fd7-1fdb-43d5-a2a8-d20fe4bcab84"
    }
    headers = {
        "Content-Type": "application/json"
    }
    try:
        response = requests.post(url, json=payload, headers=headers, timeout=30)
    except requests.RequestException as e:
        assert False, f"Request failed: {e}"

    assert response.status_code == 200, f"Expected status code 200 but got {response.status_code}"

    try:
        resp_json = response.json()
    except ValueError:
        assert False, "Response is not a valid JSON"

    assert "accessToken" in resp_json and resp_json["accessToken"], "accessToken not found or empty in response"
    assert "refreshToken" in resp_json and resp_json["refreshToken"], "refreshToken not found or empty in response"

test_post_v1_auth_login_with_valid_credentials()