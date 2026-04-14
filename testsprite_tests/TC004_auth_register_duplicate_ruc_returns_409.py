import requests

def test_auth_register_duplicate_ruc_returns_409():
    base_url = "http://localhost:80"
    endpoint = "/v1/auth/register"
    url = base_url + endpoint
    payload = {
        "ruc": "20888999001",
        "razonSocial": "Duplicate",
        "adminEmail": "dup@test.pe",
        "adminPassword": "Dup2026!",
        "adminFullName": "Dup"
    }
    headers = {
        "Content-Type": "application/json"
    }

    try:
        response = requests.post(url, json=payload, headers=headers, timeout=30)
    except requests.RequestException as e:
        assert False, f"Request failed: {e}"

    assert response.status_code == 409, f"Expected 409 Conflict, got {response.status_code}"
    try:
        json_data = response.json()
    except ValueError:
        assert False, "Response is not a valid JSON"
    assert 'error' in json_data, "Response JSON does not contain 'error' field"

test_auth_register_duplicate_ruc_returns_409()