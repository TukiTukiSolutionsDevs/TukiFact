import requests

BASE_URL = "http://localhost:80"
LOGIN_URL = f"{BASE_URL}/v1/auth/login"
CUSTOMERS_URL = f"{BASE_URL}/v1/customers"
TIMEOUT = 30

def test_customers_create_dni_customer():
    login_payload = {
        "email": "prdtest@test.pe",
        "password": "PrdTest2026!",
        "tenantId": "b2ba4cad-45c9-4a98-96f9-bf2eb6c96572"
    }
    headers = {"Content-Type": "application/json"}

    # Authenticate to get access token
    response = requests.post(LOGIN_URL, json=login_payload, headers=headers, timeout=TIMEOUT)
    assert response.status_code == 200, f"Login failed with status {response.status_code}: {response.text}"
    login_data = response.json()
    access_token = login_data.get("accessToken")
    assert access_token and access_token.startswith("eyJ"), "Invalid access token in login response"

    auth_headers = {
        "Authorization": f"Bearer {access_token}",
        "Content-Type": "application/json"
    }

    customer_payload = {
        "docType": "dni",
        "docNumber": "71234568",
        "name": "María López García",
        "email": "maria@test.pe",
        "phone": "999888777",
        "category": "nuevo"
    }

    # Create customer
    response = requests.post(CUSTOMERS_URL, json=customer_payload, headers=auth_headers, timeout=TIMEOUT)
    assert response.status_code == 201, f"Customer creation failed with status {response.status_code}: {response.text}"
    customer_data = response.json()
    customer_id = customer_data.get("id")
    assert customer_id, "Customer ID not found in response"
    
    # Validate returned data matches input for fields
    assert customer_data.get("docType") == customer_payload["docType"]
    assert customer_data.get("docNumber") == customer_payload["docNumber"]
    assert customer_data.get("name") == customer_payload["name"]

    # Cleanup: delete customer after test
    try:
        pass
    finally:
        del_response = requests.delete(f"{CUSTOMERS_URL}/{customer_id}", headers=auth_headers, timeout=TIMEOUT)
        # According to docs, 204 is expected, 400 if linked docs exists are OK too
        assert del_response.status_code in (204, 400)

test_customers_create_dni_customer()
