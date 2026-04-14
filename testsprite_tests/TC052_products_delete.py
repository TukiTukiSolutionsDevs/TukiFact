import requests

BASE_URL = "http://localhost:80"
LOGIN_URL = f"{BASE_URL}/v1/auth/login"
PRODUCTS_URL = f"{BASE_URL}/v1/products"

def test_products_delete_tc052():
    login_payload = {
        "email": "prdtest@test.pe",
        "password": "PrdTest2026!",
        "tenantId": "b2ba4cad-45c9-4a98-96f9-bf2eb6c96572"
    }

    # Authenticate and get access token
    try:
        login_resp = requests.post(LOGIN_URL, json=login_payload, timeout=30)
        assert login_resp.status_code == 200, f"Login failed with status {login_resp.status_code}"
        login_data = login_resp.json()
        access_token = login_data.get("accessToken")
        assert access_token and access_token.startswith("eyJ"), "Invalid accessToken format"
    except Exception as e:
        raise AssertionError(f"Authentication failed: {str(e)}")

    headers = {
        "Authorization": f"Bearer {access_token}"
    }

    # Using product ID from TC048 (service)
    # TC048 product data: code=SERV-001, description=Servicio de consultoría mensual
    # Since test independence is critical, create the product first

    product_payload = {
        "code": f"SERV-001-TC052",
        "description": "Servicio de consultoría mensual para TC052",
        "unitPrice": 847.46,
        "unitPriceWithIgv": 1000.00,
        "currency": "PEN",
        "igvType": "10",
        "unitMeasure": "ZZ",
        "category": "Servicios"
    }

    product_id = None
    try:
        # Create product
        create_resp = requests.post(PRODUCTS_URL, json=product_payload, headers=headers, timeout=30)
        assert create_resp.status_code == 201, f"Product creation failed with status {create_resp.status_code}"
        create_data = create_resp.json()
        product_id = create_data.get("id")
        assert product_id, "Product ID not returned"

        # DELETE /v1/products/{id}
        delete_resp = requests.delete(f"{PRODUCTS_URL}/{product_id}", headers=headers, timeout=30)
        assert delete_resp.status_code == 200, f"Delete failed with status {delete_resp.status_code}"
        delete_json = delete_resp.json()
        message = delete_json.get("message", "").lower()
        assert "eliminado" in message, f"Delete message does not contain 'eliminado': {message}"

        # Then GET /v1/products/{id} expected 404
        get_resp = requests.get(f"{PRODUCTS_URL}/{product_id}", headers=headers, timeout=30)
        assert get_resp.status_code == 404, f"Expected 404 after delete, got {get_resp.status_code}"
    finally:
        # Cleanup in case delete failed or partial failure to ensure no left overs
        if product_id:
            try:
                requests.delete(f"{PRODUCTS_URL}/{product_id}", headers=headers, timeout=30)
            except Exception:
                pass


test_products_delete_tc052()