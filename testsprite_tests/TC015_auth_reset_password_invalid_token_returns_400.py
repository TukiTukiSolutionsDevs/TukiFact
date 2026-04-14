import requests

def test_auth_reset_password_invalid_token_returns_400():
    base_url = "http://localhost:80"
    url = f"{base_url}/v1/auth/reset-password"
    headers = {
        "Content-Type": "application/json"
    }
    payload = {
        "token": "fake-invalid-reset-token",
        "newPassword": "NewPass2026!"
    }
    try:
        response = requests.post(url, json=payload, headers=headers, timeout=30)
    except requests.RequestException as e:
        assert False, f"Request failed: {e}"

    assert response.status_code == 400, f"Expected status code 400, got {response.status_code}"

    try:
        json_resp = response.json()
    except ValueError:
        assert False, "Response is not valid JSON"

    assert "error" in json_resp, "Response JSON does not contain 'error' field"

test_auth_reset_password_invalid_token_returns_400()