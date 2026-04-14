import requests
import time

BASE_URL = "http://localhost:80"
LOGIN_PAYLOAD = {
    "email": "prdtest@test.pe",
    "password": "PrdTest2026!",
    "tenantId": "b2ba4cad-45c9-4a98-96f9-bf2eb6c96572"
}
HEADERS = {
    "Content-Type": "application/json"
}


def test_customers_update():
    try:
        # Step 0: Authenticate and retrieve access token
        login_resp = requests.post(
            f"{BASE_URL}/v1/auth/login",
            json=LOGIN_PAYLOAD,
            headers=HEADERS,
            timeout=30,
        )
        assert login_resp.status_code == 200, f"Login failed: {login_resp.text}"
        login_json = login_resp.json()
        access_token = login_json.get("accessToken")
        assert isinstance(access_token, str) and access_token.startswith("eyJ"), "Invalid accessToken"
        auth_headers = {
            "Authorization": f"Bearer {access_token}",
            "Content-Type": "application/json"
        }

        # Step 1: Create a customer
        create_payload = {
            "docType": "6",
            "docNumber": "20000000044",
            "name": "Update Test Customer",
            "email": "update@test.pe",
        }
        create_resp = requests.post(
            f"{BASE_URL}/v1/customers",
            json=create_payload,
            headers=auth_headers,
            timeout=30,
        )
        assert create_resp.status_code == 201, f"Customer creation failed: {create_resp.text}"
        create_json = create_resp.json()
        customer_id = create_json.get("id")
        assert isinstance(customer_id, str) and len(customer_id) > 0, "Customer id missing"

        # Step 2: Update the customer with PUT /v1/customers/{id}
        update_payload = {
            "name": "Updated Customer Name",
            "email": "updated@test.pe"
        }
        update_resp = requests.put(
            f"{BASE_URL}/v1/customers/{customer_id}",
            json=update_payload,
            headers=auth_headers,
            timeout=30,
        )
        assert update_resp.status_code == 200, f"Customer update failed: {update_resp.text}"

        # Step 3: Get the customer to verify changes
        get_resp = requests.get(
            f"{BASE_URL}/v1/customers/{customer_id}",
            headers=auth_headers,
            timeout=30,
        )
        assert get_resp.status_code == 200, f"Get customer failed: {get_resp.text}"
        get_json = get_resp.json()
        assert get_json.get("id") == customer_id, "Customer ID mismatch"
        assert get_json.get("name") == "Updated Customer Name", "Name not updated"
        assert get_json.get("email") == "updated@test.pe", "Email not updated"

    finally:
        # Cleanup: Delete the created customer
        if 'customer_id' in locals():
            try:
                requests.delete(
                    f"{BASE_URL}/v1/customers/{customer_id}",
                    headers=auth_headers,
                    timeout=30,
                )
            except Exception:
                pass


test_customers_update()