import requests
import time

BASE_URL = "http://localhost:80"
LOGIN_URL = f"{BASE_URL}/v1/auth/login"
CUSTOMERS_URL = f"{BASE_URL}/v1/customers"

def test_customers_get_by_id():
    login_payload = {
        "email": "prdtest@test.pe",
        "password": "PrdTest2026!",
        "tenantId": "b2ba4cad-45c9-4a98-96f9-bf2eb6c96572"
    }
    timeout = 30

    # Step 1: Authenticate and get access token
    try:
        login_response = requests.post(LOGIN_URL, json=login_payload, timeout=timeout)
        assert login_response.status_code == 200, f"Login failed with status code {login_response.status_code}"
        login_json = login_response.json()
        access_token = login_json.get("accessToken")
        assert isinstance(access_token, str) and access_token.startswith("eyJ"), "Invalid accessToken format"
    except Exception as e:
        raise AssertionError(f"Login request failed or invalid response: {str(e)}")

    headers = {
        "Authorization": f"Bearer {access_token}",
        "Content-Type": "application/json"
    }

    customer_payload = {
        "docType": "6",
        "docNumber": "20000000042",
        "name": "Get By ID Test Customer",
        "email": "getbyid@test.pe"
    }

    customer_id = None
    try:
        # Step 2: Create a customer
        create_response = requests.post(CUSTOMERS_URL, json=customer_payload, headers=headers, timeout=timeout)
        assert create_response.status_code == 201, f"Create customer failed with status code {create_response.status_code}"
        create_json = create_response.json()
        customer_id = create_json.get("id")
        assert isinstance(customer_id, str) and len(customer_id) > 0, "Customer ID missing or invalid"

        # Step 3: Get the customer by ID
        get_url = f"{CUSTOMERS_URL}/{customer_id}"
        get_response = requests.get(get_url, headers=headers, timeout=timeout)
        assert get_response.status_code == 200, f"Get customer by ID failed with status code {get_response.status_code}"
        get_json = get_response.json()

        # Validate fields
        assert get_json.get("id") == customer_id, "Customer ID does not match"
        assert get_json.get("docType") == "6", "docType does not match"
        assert get_json.get("docNumber") == customer_payload["docNumber"], "docNumber does not match"
        assert get_json.get("name") == customer_payload["name"], "name does not match"

    finally:
        # Cleanup: delete the created customer if created
        if customer_id:
            try:
                delete_url = f"{CUSTOMERS_URL}/{customer_id}"
                delete_response = requests.delete(delete_url, headers=headers, timeout=timeout)
                # Accept 200 or 204 as successful deletion
                assert delete_response.status_code in (200, 204), f"Delete customer failed with status code {delete_response.status_code}"
            except Exception:
                pass  # Ignore deletion errors to not mask test results

test_customers_get_by_id()