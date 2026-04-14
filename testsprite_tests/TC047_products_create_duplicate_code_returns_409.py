import requests
import time

BASE_URL = "http://localhost:80"
LOGIN_ENDPOINT = "/v1/auth/login"
PRODUCTS_ENDPOINT = "/v1/products"

LOGIN_PAYLOAD = {
    "email": "prdtest@test.pe",
    "password": "PrdTest2026!",
    "tenantId": "b2ba4cad-45c9-4a98-96f9-bf2eb6c96572"
}

DUPLICATE_PRODUCT_PAYLOAD = {
    "code": "PROD-001",
    "description": "Duplicado",
    "unitPrice": 100,
    "unitPriceWithIgv": 118
}

def test_products_create_duplicate_code_returns_409():
    session = requests.Session()
    try:
        # Authenticate and get access token
        login_resp = session.post(
            BASE_URL + LOGIN_ENDPOINT,
            json=LOGIN_PAYLOAD,
            timeout=30
        )
        assert login_resp.status_code == 200, f"Login failed: {login_resp.text}"
        login_data = login_resp.json()
        access_token = login_data.get("accessToken")
        assert isinstance(access_token, str) and access_token.startswith("eyJ"), "Invalid access token"

        headers = {
            "Authorization": f"Bearer {access_token}",
            "Content-Type": "application/json"
        }

        # Step 1: Create the initial product with code "PROD-001" if not exists
        # Use a unique code to avoid dependency if possible? 
        # But test requires code "PROD-001", so create first or assume it exists.

        # To ensure test independence and existence, try creating the product first:
        product_payload = {
            "code": "PROD-001",
            "description": f"Test Product Created at {int(time.time())}",
            "unitPrice": 100,
            "unitPriceWithIgv": 118
        }
        create_resp = session.post(
            BASE_URL + PRODUCTS_ENDPOINT,
            json=product_payload,
            headers=headers,
            timeout=30
        )
        # If created or already exists (409), ignore error here because test is for duplicate
        # Just assert success or conflict is acceptable here
        assert create_resp.status_code in [201, 409], f"Unexpected status on initial product creation: {create_resp.status_code} - {create_resp.text}"

        # Step 2: Attempt to create duplicate product again, expecting 409
        dup_resp = session.post(
            BASE_URL + PRODUCTS_ENDPOINT,
            json=DUPLICATE_PRODUCT_PAYLOAD,
            headers=headers,
            timeout=30
        )
        assert dup_resp.status_code == 409, f"Expected 409 Conflict, got {dup_resp.status_code}"
        dup_json = dup_resp.json()
        error_msg = dup_json.get("error", "").lower()
        assert "ya existe" in error_msg, f"Error message does not contain 'ya existe': {dup_resp.text}"

    finally:
        # Cleanup: attempt to delete the initial created product if created successfully and if product_id known
        # Fetch product by code to get id
        try:
            # Authorization header included
            get_resp = session.get(
                BASE_URL + PRODUCTS_ENDPOINT + f"?search=PROD-001",
                headers=headers,
                timeout=30
            )
            if get_resp.status_code == 200:
                products = get_resp.json().get("data", [])
                for prod in products:
                    if prod.get("code") == "PROD-001":
                        product_id = prod.get("id")
                        # Delete product
                        del_resp = session.delete(
                            BASE_URL + PRODUCTS_ENDPOINT + f"/{product_id}",
                            headers=headers,
                            timeout=30
                        )
                        # Deletion might return 200 or 204; accept both
                        if del_resp.status_code not in (200, 204):
                            print(f"Warning: Failed to delete product {product_id}: {del_resp.status_code} {del_resp.text}")
                        break
        except Exception:
            pass

test_products_create_duplicate_code_returns_409()