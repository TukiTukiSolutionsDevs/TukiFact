import requests

def test_auth_refresh_invalid_token_returns_401():
    base_url = "http://localhost:80"
    url = f"{base_url}/v1/auth/refresh"
    headers = {
        "Content-Type": "application/json"
    }
    json_data = {
        "refreshToken": "invalid-garbage-token-12345"
    }

    try:
        response = requests.post(url, json=json_data, headers=headers, timeout=30)
    except requests.RequestException as e:
        assert False, f"Request failed: {e}"

    assert response.status_code == 401, f"Expected 401 Unauthorized, got {response.status_code}"
    try:
        data = response.json()
    except ValueError:
        # if no JSON returned, that's acceptable but verify if possible
        data = None

    if data is not None:
        assert 'error' in data or data == {}, "Response JSON should contain 'error' field or be empty for 401 response"

test_auth_refresh_invalid_token_returns_401()