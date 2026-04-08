import requests

BASE_URL = "http://localhost:5186"
REGISTER_ENDPOINT = "/v1/auth/register"
TIMEOUT = 30

def test_post_v1_auth_register_tenant_and_admin_user():
    url = BASE_URL + REGISTER_ENDPOINT
    payload = {
        "ruc": "20777777771",
        "razonSocial": "New Registered Company SAC",
        "nombreComercial": "NewRegCo",
        "direccion": "Av Registro 123",
        "adminEmail": "newreg@test.pe",
        "adminPassword": "Password123!",
        "adminFullName": "New Register Admin"
    }
    headers = {"Content-Type": "application/json"}

    response = None
    try:
        response = requests.post(url, json=payload, headers=headers, timeout=TIMEOUT)
    except requests.RequestException as e:
        assert False, f"Request to register tenant failed: {e}"

    assert response.status_code == 201, f"Expected status code 201, got {response.status_code}"
    json_resp = None
    try:
        json_resp = response.json()
    except Exception:
        assert False, "Response is not valid JSON"

    assert "accessToken" in json_resp and json_resp["accessToken"], "accessToken missing or empty in response"
    assert "refreshToken" in json_resp and json_resp["refreshToken"], "refreshToken missing or empty in response"


test_post_v1_auth_register_tenant_and_admin_user()