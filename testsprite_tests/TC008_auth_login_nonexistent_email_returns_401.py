import requests

def test_auth_login_nonexistent_email_returns_401():
    base_url = "http://localhost:80"
    url = f"{base_url}/v1/auth/login"
    payload = {
        "email": "doesnotexist@fake.pe",
        "password": "Whatever2026!",
        "tenantId": "b2ba4cad-45c9-4a98-96f9-bf2eb6c96572"
    }
    headers = {
        "Content-Type": "application/json"
    }
    try:
        resp = requests.post(url, json=payload, headers=headers, timeout=30)
    except requests.RequestException as e:
        assert False, f"Request failed: {e}"

    assert resp.status_code == 401, f"Expected 401 Unauthorized but got {resp.status_code}"
    
    try:
        data = resp.json()
    except ValueError:
        data = None
    
    # Check if error field exists or response body is empty (some APIs send empty body with 401)
    # Since instructions just say expected 401 Unauthorized, no specific body asserted,
    # but usually error field is present.
    if data is not None:
        assert isinstance(data, dict), "Response JSON is not a dictionary"
        # It's common to have an 'error' or 'message' field for auth errors, validate if present
        assert any(key in data for key in ["error", "message"]), "Expected error or message field in response JSON"

test_auth_login_nonexistent_email_returns_401()