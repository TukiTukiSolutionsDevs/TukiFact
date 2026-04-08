import requests

def test_login_wrong_password_returns_401():
    base_url = "http://localhost:5186"
    url = f"{base_url}/v1/auth/login"
    headers = {
        "Content-Type": "application/json"
    }
    payload = {
        "email": "admin@tukitest.pe",
        "password": "wrongpassword",
        "tenantId": "0c700fd7-1fdb-43d5-a2a8-d20fe4bcab84"
    }
    try:
        response = requests.post(url, json=payload, headers=headers, timeout=30)
    except requests.RequestException as e:
        assert False, f"Request failed: {e}"

    assert response.status_code == 401, f"Expected status code 401, got {response.status_code}"

test_login_wrong_password_returns_401()
