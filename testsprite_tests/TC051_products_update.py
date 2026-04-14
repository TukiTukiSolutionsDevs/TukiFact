import requests
import time

BASE_URL = "http://localhost:80"
LOGIN_ENDPOINT = "/v1/auth/login"
PRODUCTS_ENDPOINT = "/v1/products"
AUTH_PAYLOAD = {
    "email": "prdtest@test.pe",
    "password": "PrdTest2026!",
    "tenantId": "b2ba4cad-45c9-4a98-96f9-bf2eb6c96572"
}
TIMEOUT = 30


def test_products_update():
    access_token = None
    product_id = None

    try:
        # Step 0: Authenticate and get accessToken
        login_resp = requests.post(
            f"{BASE_URL}{LOGIN_ENDPOINT}",
            json=AUTH_PAYLOAD,
            timeout=TIMEOUT
        )
        assert login_resp.status_code == 200, f"Login failed, status {login_resp.status_code}"
        login_json = login_resp.json()
        assert "accessToken" in login_json and isinstance(login_json["accessToken"], str) and login_json["accessToken"].startswith("eyJ"), "No valid accessToken in login response"
        access_token = login_json["accessToken"]

        headers = {
            "Authorization": f"Bearer {access_token}",
            "Content-Type": "application/json"
        }

        # Step 1: Create a product
        product_create_body = {
            "code": f"UPD-051-{int(time.time())}",
            "description": "Update Test Product",
            "unitPrice": 30.00,
            "unitPriceWithIgv": 35.40,
            "currency": "PEN"
        }
        create_resp = requests.post(
            f"{BASE_URL}{PRODUCTS_ENDPOINT}",
            headers=headers,
            json=product_create_body,
            timeout=TIMEOUT
        )
        assert create_resp.status_code == 201, f"Product creation failed, status {create_resp.status_code}, body: {create_resp.text}"
        create_json = create_resp.json()
        assert "id" in create_json and create_json["id"], "No product id in creation response"
        product_id = create_json["id"]

        # Step 2: Update the product
        product_update_body = {
            "description": "Updated Product Name",
            "unitPrice": 35.00,
            "category": "Updated"
        }
        update_resp = requests.put(
            f"{BASE_URL}{PRODUCTS_ENDPOINT}/{product_id}",
            headers=headers,
            json=product_update_body,
            timeout=TIMEOUT
        )
        assert update_resp.status_code == 200, f"Product update failed, status {update_resp.status_code}, body: {update_resp.text}"

        # Step 3: Get the product to verify updates
        get_resp = requests.get(
            f"{BASE_URL}{PRODUCTS_ENDPOINT}/{product_id}",
            headers=headers,
            timeout=TIMEOUT
        )
        assert get_resp.status_code == 200, f"Get product failed, status {get_resp.status_code}, body: {get_resp.text}"
        product_data = get_resp.json()
        assert product_data.get("description") == "Updated Product Name", f"Product description not updated: {product_data.get('description')}"
        # unitPrice should match 35.00, allow float epsilon tolerance
        assert abs(float(product_data.get("unitPrice", 0)) - 35.00) < 0.0001, f"Product unitPrice not updated: {product_data.get('unitPrice')}"

    finally:
        # Cleanup: delete the product if created
        if access_token and product_id:
            headers = {
                "Authorization": f"Bearer {access_token}"
            }
            # Best effort delete, ignore errors
            try:
                requests.delete(f"{BASE_URL}{PRODUCTS_ENDPOINT}/{product_id}", headers=headers, timeout=TIMEOUT)
            except Exception:
                pass


test_products_update()