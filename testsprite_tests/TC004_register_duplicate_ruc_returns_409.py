import requests

def test_register_duplicate_ruc_returns_409():
    base_url = "http://localhost:5186"
    endpoint = "/v1/auth/register"
    url = f"{base_url}{endpoint}"
    headers = {"Content-Type": "application/json"}

    payload = {
        "ruc": "20100070970",
        "razonSocial": "Duplicate RUC",
        "adminEmail": "duptest@test.pe",
        "adminPassword": "Dup2026!",
        "adminFullName": "Dup"
    }

    try:
        response = requests.post(url, json=payload, headers=headers, timeout=30)
    except requests.RequestException as e:
        assert False, f"Request failed: {e}"

    assert response.status_code == 409, (
        f"Expected status code 409, got {response.status_code}. "
        f"Response content: {response.text}"
    )

test_register_duplicate_ruc_returns_409()