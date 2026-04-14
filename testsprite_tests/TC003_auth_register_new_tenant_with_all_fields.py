import requests

BASE_URL = "http://localhost:80"
REGISTER_ENDPOINT = "/v1/auth/register"
TIMEOUT = 30

def test_auth_register_new_tenant_with_all_fields():
    url = BASE_URL + REGISTER_ENDPOINT
    payload = {
        "ruc": "20888999001",
        "razonSocial": "PRD Test Enterprise SAC",
        "nombreComercial": "PRD Test",
        "direccion": "Av Testing 456, Lima",
        "adminEmail": "prdtest@test.pe",
        "adminPassword": "PrdTest2026!",
        "adminFullName": "PRD Admin User"
    }
    headers = {
        "Content-Type": "application/json"
    }
    try:
        response = requests.post(url, json=payload, headers=headers, timeout=TIMEOUT)
    except requests.RequestException as e:
        assert False, f"Request failed: {e}"

    assert response.status_code == 201, f"Expected status code 201, got {response.status_code}"

    try:
        data = response.json()
    except ValueError:
        assert False, "Response is not a valid JSON"

    access_token = data.get("accessToken")
    refresh_token = data.get("refreshToken")
    user = data.get("user")

    assert isinstance(access_token, str) and access_token.startswith("eyJ") and len(access_token) > 0, "Invalid accessToken"
    assert isinstance(refresh_token, str) and len(refresh_token) > 0, "Invalid refreshToken"
    assert isinstance(user, dict), "User key missing or invalid"

    assert user.get("email") == "prdtest@test.pe", f"Expected user email 'prdtest@test.pe', got {user.get('email')}"
    assert user.get("role") == "admin", f"Expected user role 'admin', got {user.get('role')}"
    assert user.get("fullName") == "PRD Admin User", f"Expected user fullName 'PRD Admin User', got {user.get('fullName')}"

    # Save accessToken for TC004 (this demo just prints; replace with actual storage if needed)
    print("Saved accessToken for TC004:", access_token)

test_auth_register_new_tenant_with_all_fields()
