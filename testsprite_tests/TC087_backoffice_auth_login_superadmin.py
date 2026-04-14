import requests

BACKOFFICE_TOKEN = None

def test_backoffice_auth_login_superadmin():
    global BACKOFFICE_TOKEN
    base_url = "http://localhost:80"
    url = f"{base_url}/v1/backoffice/auth/login"
    payload = {
        "email": "superadmin@tukifact.net.pe",
        "password": "SuperAdmin2026!"
    }
    headers = {
        "Content-Type": "application/json"
    }
    try:
        response = requests.post(url, json=payload, headers=headers, timeout=30)
    except requests.RequestException as e:
        assert False, f"Request failed: {e}"

    assert response.status_code == 200, f"Expected 200 OK, got {response.status_code}"
    try:
        data = response.json()
    except ValueError:
        assert False, "Response is not a valid JSON"

    access_token = data.get("accessToken")
    user = data.get("user")

    assert access_token is not None and isinstance(access_token, str), "Missing or invalid 'accessToken'"
    assert access_token.startswith("eyJ") and len(access_token) > 10, "Invalid 'accessToken' format"
    assert user is not None and isinstance(user, dict), "Missing or invalid 'user' object"
    role = user.get("role")
    assert role == "superadmin", f"Expected user role 'superadmin', got '{role}'"

    BACKOFFICE_TOKEN = access_token

test_backoffice_auth_login_superadmin()