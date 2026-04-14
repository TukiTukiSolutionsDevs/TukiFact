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
LOGIN_TIMEOUT = 30
PRODUCTS_TIMEOUT = 30

def test_TC046_products_create_with_all_fields():
    access_token = None
    product_id = None
    try:
        # Authenticate and get Bearer token
        login_resp = requests.post(
            f"{BASE_URL}{LOGIN_ENDPOINT}",
            json=LOGIN_PAYLOAD,
            timeout=LOGIN_TIMEOUT
        )
        assert login_resp.status_code == 200, f"Login failed with status {login_resp.status_code}"
        login_json = login_resp.json()
        assert "accessToken" in login_json, "accessToken missing in login response"
        access_token = login_json["accessToken"]
        assert isinstance(access_token, str) and access_token.startswith("eyJ"), "Access token invalid format"

        headers = {
            "Authorization": f"Bearer {access_token}",
            "Content-Type": "application/json"
        }

        # Create product payload with unique code to ensure test independence
        unique_code = f"PROD-{int(time.time())}"
        product_payload = {
            "code": unique_code,
            "description": "Laptop HP Pavilion 15",
            "unitPrice": 2542.37,
            "unitPriceWithIgv": 3000.00,
            "sunatCode": "43211503",
            "currency": "PEN",
            "igvType": "10",
            "unitMeasure": "NIU",
            "category": "Tecnología",
            "brand": "HP"
        }

        # POST /v1/products to create a new product
        product_resp = requests.post(
            f"{BASE_URL}{PRODUCTS_ENDPOINT}",
            json=product_payload,
            headers=headers,
            timeout=PRODUCTS_TIMEOUT
        )
        assert product_resp.status_code == 201, f"Product creation failed with status {product_resp.status_code}"
        product_json = product_resp.json()

        # Validate required fields in response
        assert "id" in product_json, "Product id missing in response"
        assert product_json.get("code") == unique_code, f"Product code mismatch: expected {unique_code}, got {product_json.get('code')}"
        assert product_json.get("description") == product_payload["description"], "Product description mismatch"

        product_id = product_json["id"]
    finally:
        # Clean up: Delete the created product if exists
        if product_id and access_token:
            try:
                headers = {
                    "Authorization": f"Bearer {access_token}"
                }
                del_resp = requests.delete(
                    f"{BASE_URL}{PRODUCTS_ENDPOINT}/{product_id}",
                    headers=headers,
                    timeout=30
                )
                assert del_resp.status_code in (200, 204), f"Failed to delete product {product_id}"
            except Exception:
                pass

test_TC046_products_create_with_all_fields()