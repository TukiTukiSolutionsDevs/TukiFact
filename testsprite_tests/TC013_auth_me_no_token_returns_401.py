import requests

def test_auth_me_no_token_returns_401():
    base_url = "http://localhost:80"
    url = f"{base_url}/v1/auth/me"
    headers = {}  # No Authorization header

    try:
        response = requests.get(url, headers=headers, timeout=30)
    except requests.RequestException as e:
        assert False, f"Request to {url} failed: {e}"

    assert response.status_code == 401, f"Expected status code 401, got {response.status_code}"


test_auth_me_no_token_returns_401()