import requests

BASE_URL = "http://localhost:5186"

def test_auth_me_no_token_returns_401():
    url = f"{BASE_URL}/v1/auth/me"
    headers = {
        "Content-Type": "application/json"
        # Note: No Authorization header included intentionally
    }
    try:
        response = requests.get(url, headers=headers, timeout=30)
        # Expect 401 Unauthorized because no token provided
        assert response.status_code == 401, f"Expected 401 Unauthorized, got {response.status_code}"
    except requests.RequestException as e:
        assert False, f"Request failed: {e}"

test_auth_me_no_token_returns_401()