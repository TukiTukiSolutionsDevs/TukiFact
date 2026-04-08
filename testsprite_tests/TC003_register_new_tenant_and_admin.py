import requests

def test_register_new_tenant_and_admin():
    base_url = "http://localhost:5186"
    url = f"{base_url}/v1/auth/register"
    headers = {
        "Content-Type": "application/json"
    }
    payload = {
        "ruc": "20777111881",
        "razonSocial": "New Backend Co SAC",
        "adminEmail": "newbe@test.pe",
        "adminPassword": "BackendTest2026!",
        "adminFullName": "Backend Tester"
    }
    try:
        response = requests.post(url, json=payload, headers=headers, timeout=30)
    except requests.RequestException as e:
        assert False, f"Request failed: {e}"

    assert response.status_code == 201, f"Expected status code 201, got {response.status_code}"
    try:
        json_data = response.json()
    except ValueError:
        assert False, "Response is not valid JSON"

    assert "accessToken" in json_data, "'accessToken' not found in response JSON"
    assert "refreshToken" in json_data, "'refreshToken' not found in response JSON"
    assert "user" in json_data, "'user' object not found in response JSON"

    user = json_data["user"]
    assert isinstance(user, dict), "'user' should be an object"
    assert user.get("email") == "newbe@test.pe", f"Expected user.email to be 'newbe@test.pe', got '{user.get('email')}'"
    assert user.get("role") == "admin", f"Expected user.role to be 'admin', got '{user.get('role')}'"


test_register_new_tenant_and_admin()