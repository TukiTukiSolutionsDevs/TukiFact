import requests
import time

def test_products_create_service_product():
    base_url = "http://localhost:80"
    login_url = f"{base_url}/v1/auth/login"
    products_url = f"{base_url}/v1/products"
    timeout = 30
    
    login_payload = {
        "email": "prdtest@test.pe",
        "password": "PrdTest2026!",
        "tenantId": "b2ba4cad-45c9-4a98-96f9-bf2eb6c96572"
    }
    
    product_payload = {
        "code": "SERV-001",
        "description": "Servicio de consultoría mensual",
        "unitPrice": 847.46,
        "unitPriceWithIgv": 1000.00,
        "currency": "PEN",
        "igvType": "10",
        "unitMeasure": "ZZ",
        "category": "Servicios"
    }
    
    access_token = None
    product_id = None
    
    try:
        # Login to get token
        login_resp = requests.post(login_url, json=login_payload, timeout=timeout)
        assert login_resp.status_code == 200, f"Login failed: {login_resp.status_code} - {login_resp.text}"
        login_json = login_resp.json()
        assert isinstance(login_json.get("accessToken"), str) and login_json.get("accessToken").startswith("eyJ"), "Invalid accessToken in response"
        access_token = login_json["accessToken"]
        assert access_token, "Empty access token"
        
        headers = {
            "Authorization": f"Bearer {access_token}",
            "Content-Type": "application/json"
        }
        
        # Create new product
        product_resp = requests.post(products_url, json=product_payload, headers=headers, timeout=timeout)
        assert product_resp.status_code == 201, f"Product creation failed: {product_resp.status_code} - {product_resp.text}"
        product_json = product_resp.json()
        product_id = product_json.get("id")
        assert product_id, "Product ID not found in response"
        assert product_json.get("code") == "SERV-001", "Product code mismatch"
        assert product_json.get("description") == "Servicio de consultoría mensual", "Product description mismatch"
    
    finally:
        # Cleanup: delete the created product if created
        if access_token and product_id:
            try:
                del_resp = requests.delete(f"{products_url}/{product_id}", headers={"Authorization": f"Bearer {access_token}"}, timeout=timeout)
                # Deletion may return 200 or 204. If failure, we ignore here.
            except Exception:
                pass

test_products_create_service_product()