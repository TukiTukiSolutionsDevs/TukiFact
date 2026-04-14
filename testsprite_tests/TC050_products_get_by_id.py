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
TIMEOUT = 30


def test_products_get_by_id():
    # Step 0: Authenticate and get access token
    try:
        login_resp = requests.post(
            BASE_URL + LOGIN_ENDPOINT,
            json=LOGIN_PAYLOAD,
            timeout=TIMEOUT
        )
        assert login_resp.status_code == 200, f"Login failed with status {login_resp.status_code}"
        login_json = login_resp.json()
        access_token = login_json.get("accessToken")
        assert isinstance(access_token, str) and access_token.startswith("eyJ"), "Invalid accessToken format"

        headers = {
            "Authorization": f"Bearer {access_token}",
            "Content-Type": "application/json"
        }

        # Step 1: Create a product
        product_payload = {
            "code": "GBI-050",
            "description": "Get By ID Test Product",
            "unitPrice": 25.00,
            "unitPriceWithIgv": 29.50,
            "currency": "PEN",
            "category": "Test"
        }
        create_resp = requests.post(
            BASE_URL + PRODUCTS_ENDPOINT,
            json=product_payload,
            headers=headers,
            timeout=TIMEOUT
        )
        assert create_resp.status_code == 201, f"Product creation failed with status {create_resp.status_code}"
        create_json = create_resp.json()
        product_id = create_json.get("id")
        assert isinstance(product_id, str) and len(product_id) > 0, "Product ID missing or invalid"

        # Step 2: Get the product by ID
        get_resp = requests.get(
            f"{BASE_URL}{PRODUCTS_ENDPOINT}/{product_id}",
            headers=headers,
            timeout=TIMEOUT
        )
        assert get_resp.status_code == 200, f"Get product by ID failed with status {get_resp.status_code}"
        product_data = get_resp.json()

        # Validate response fields
        assert "id" in product_data and product_data["id"] == product_id, "Product ID mismatch"
        assert product_data.get("code") == "GBI-050", "Product code mismatch"
        assert product_data.get("description") == "Get By ID Test Product", "Product description mismatch"
        # Floating point comparison within a very small margin
        assert abs(product_data.get("unitPrice", 0) - 25.00) < 0.001, f"unitPrice mismatch: {product_data.get('unitPrice')}"
        assert product_data.get("category") == "Test", "Product category mismatch"
        assert product_data.get("isActive") is True, "Product isActive should be True"

    finally:
        # Cleanup: Delete the product if it was created
        if 'access_token' in locals() and 'product_id' in locals():
            try:
                del_resp = requests.delete(
                    f"{BASE_URL}{PRODUCTS_ENDPOINT}/{product_id}",
                    headers={"Authorization": f"Bearer {access_token}"},
                    timeout=TIMEOUT
                )
                # It's OK if delete fails but we do not fail the test here
            except Exception:
                pass


test_products_get_by_id()