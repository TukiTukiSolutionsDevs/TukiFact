import requests
import uuid

BASE_URL = "http://localhost:80"
LOGIN_ENDPOINT = "/v1/auth/login"
CUSTOMERS_ENDPOINT = "/v1/customers"
TIMEOUT = 30


def test_customers_create_ruc_customer():
    login_payload = {
        "email": "prdtest@test.pe",
        "password": "PrdTest2026!",
        "tenantId": "b2ba4cad-45c9-4a98-96f9-bf2eb6c96572"
    }

    customer_payload = {
        "docType": "6",
        "docNumber": "20567890123",
        "name": "Cliente Empresa SAC",
        "email": "empresa@test.pe",
        "phone": "01-2345678",
        "address": "Av Cliente 789, Lima",
        "ubigeo": "150101",
        "departamento": "Lima",
        "provincia": "Lima",
        "distrito": "Lima",
        "category": "frecuente",
        "notes": "Cliente de prueba PRD"
    }

    # Authenticate to get access token
    try:
        login_resp = requests.post(
            BASE_URL + LOGIN_ENDPOINT,
            json=login_payload,
            timeout=TIMEOUT
        )
        login_resp.raise_for_status()
    except requests.RequestException as e:
        assert False, f"Login request failed: {e}"

    login_json = login_resp.json()
    access_token = login_json.get("accessToken")
    assert isinstance(access_token, str) and access_token.startswith("eyJ"), "Invalid accessToken"

    headers = {
        "Authorization": f"Bearer {access_token}",
        "Content-Type": "application/json",
        "Accept": "application/json"
    }

    customer_id = None
    # Create customer
    try:
        create_resp = requests.post(
            BASE_URL + CUSTOMERS_ENDPOINT,
            json=customer_payload,
            headers=headers,
            timeout=TIMEOUT
        )
        create_resp.raise_for_status()
        assert create_resp.status_code == 201, f"Expected 201 Created, got {create_resp.status_code}"

        create_json = create_resp.json()
        # Validate id presence and format (UUID)
        customer_id = create_json.get("id")
        assert customer_id is not None, "Response JSON missing 'id'"
        # Validate UUID format for id
        try:
            uuid.UUID(customer_id)
        except ValueError:
            assert False, "'id' is not a valid UUID"

        # Validate docNumber and name accuracy
        assert create_json.get("docNumber") == customer_payload["docNumber"], \
            f"docNumber mismatch: expected {customer_payload['docNumber']}, got {create_json.get('docNumber')}"
        assert create_json.get("name") == customer_payload["name"], \
            f"name mismatch: expected {customer_payload['name']}, got {create_json.get('name')}"

    finally:
        if customer_id:
            # Cleanup: delete the created customer to maintain test isolation
            try:
                del_resp = requests.delete(
                    f"{BASE_URL}{CUSTOMERS_ENDPOINT}/{customer_id}",
                    headers=headers,
                    timeout=TIMEOUT
                )
                # DELETE may return 204 or 400 if customer has associated documents.
                assert del_resp.status_code in (204, 400), \
                    f"Unexpected DELETE status code: {del_resp.status_code}"
            except requests.RequestException:
                pass


test_customers_create_ruc_customer()