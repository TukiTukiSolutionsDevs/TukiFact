import requests
import time

BASE_URL = "http://localhost:80"
LOGIN_URL = f"{BASE_URL}/v1/auth/login"
CUSTOMERS_URL = f"{BASE_URL}/v1/customers"

LOGIN_PAYLOAD = {
    "email": "prdtest@test.pe",
    "password": "PrdTest2026!",
    "tenantId": "b2ba4cad-45c9-4a98-96f9-bf2eb6c96572"
}

DUPLICATE_CUSTOMER_BODY = {
    "docType": "6",
    "docNumber": "20567890123",
    "name": "Duplicado"
}

def test_customers_create_duplicate_returns_409():
    try:
        # Authenticate to get Bearer token
        login_resp = requests.post(LOGIN_URL, json=LOGIN_PAYLOAD, timeout=30)
        assert login_resp.status_code == 200, f"Login failed with status {login_resp.status_code}"
        login_json = login_resp.json()
        access_token = login_json.get("accessToken")
        assert access_token and access_token.startswith("eyJ"), "Invalid accessToken in login response"

        headers = {
            "Authorization": f"Bearer {access_token}",
            "Content-Type": "application/json"
        }

        # Attempt to create the duplicate customer with same docNumber as in TC038
        post_resp = requests.post(CUSTOMERS_URL, json=DUPLICATE_CUSTOMER_BODY, headers=headers, timeout=30)

        assert post_resp.status_code == 409, f"Expected 409 Conflict, got {post_resp.status_code}"

        resp_json = post_resp.json()
        error_msg = resp_json.get("error", "").lower()
        assert "ya existe" in error_msg, f"Expected error message to contain 'ya existe', got: {error_msg}"

    except requests.RequestException as e:
        assert False, f"Request exception occurred: {e}"
    except AssertionError:
        raise
    except Exception as e:
        assert False, f"Unexpected exception occurred: {e}"

test_customers_create_duplicate_returns_409()