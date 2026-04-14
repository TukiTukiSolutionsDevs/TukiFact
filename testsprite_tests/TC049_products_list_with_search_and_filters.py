import requests
import time

BASE_URL = "http://localhost:80"
LOGIN_ENDPOINT = "/v1/auth/login"
PRODUCTS_ENDPOINT = "/v1/products"
TIMEOUT = 30

def test_products_list_with_search_and_filters():
    login_payload = {
        "email": "prdtest@test.pe",
        "password": "PrdTest2026!",
        "tenantId": "b2ba4cad-45c9-4a98-96f9-bf2eb6c96572"
    }
    try:
        # Step 0: Login to get the access token
        login_resp = requests.post(f"{BASE_URL}{LOGIN_ENDPOINT}", json=login_payload, timeout=TIMEOUT)
        assert login_resp.status_code == 200, f"Login failed with status {login_resp.status_code}"
        login_json = login_resp.json()
        access_token = login_json.get("accessToken")
        assert access_token and isinstance(access_token, str) and access_token.startswith("eyJ"), "Invalid accessToken"

        headers = {"Authorization": f"Bearer {access_token}"}

        # Step 1: Create 2 products first
        product1_payload = {
            "code": f"LST-P1-{int(time.time())}",
            "description": "List Test Product 1",
            "unitPrice": 50.00,
            "unitPriceWithIgv": 59.00,
            "currency": "PEN"
        }
        product2_payload = {
            "code": f"LST-P2-{int(time.time())}",
            "description": "List Test Product 2",
            "unitPrice": 100.00,
            "unitPriceWithIgv": 118.00,
            "currency": "PEN"
        }

        created_product_ids = []

        # Create first product
        resp1 = requests.post(f"{BASE_URL}{PRODUCTS_ENDPOINT}", headers=headers, json=product1_payload, timeout=TIMEOUT)
        assert resp1.status_code == 201, f"Failed to create product 1: {resp1.status_code} {resp1.text}"
        product1 = resp1.json()
        product1_id = product1.get("id")
        assert product1.get("code") == product1_payload["code"], "Product 1 code mismatch"
        created_product_ids.append(product1_id)

        # Create second product
        resp2 = requests.post(f"{BASE_URL}{PRODUCTS_ENDPOINT}", headers=headers, json=product2_payload, timeout=TIMEOUT)
        assert resp2.status_code == 201, f"Failed to create product 2: {resp2.status_code} {resp2.text}"
        product2 = resp2.json()
        product2_id = product2.get("id")
        assert product2.get("code") == product2_payload["code"], "Product 2 code mismatch"
        created_product_ids.append(product2_id)

        # Step 2: GET /v1/products?page=1&pageSize=50
        params_all = {"page": 1, "pageSize": 50}
        resp_list_all = requests.get(f"{BASE_URL}{PRODUCTS_ENDPOINT}", headers=headers, params=params_all, timeout=TIMEOUT)
        assert resp_list_all.status_code == 200, f"Failed to list products: {resp_list_all.status_code}"
        list_all_json = resp_list_all.json()
        assert "data" in list_all_json and isinstance(list_all_json["data"], list), "Missing 'data' array in response"
        assert "pagination" in list_all_json and isinstance(list_all_json["pagination"], dict), "Missing 'pagination' object"
        # The created two products should be in the list
        codes_listed = [p.get("code") for p in list_all_json["data"] if "code" in p]
        assert product1_payload["code"] in codes_listed, "Product 1 not found in product list"
        assert product2_payload["code"] in codes_listed, "Product 2 not found in product list"

        # Step 3: GET /v1/products?search=LST-P1 (using code of product 1)
        search_term = product1_payload["code"]
        params_search = {"search": search_term}
        resp_search = requests.get(f"{BASE_URL}{PRODUCTS_ENDPOINT}", headers=headers, params=params_search, timeout=TIMEOUT)
        assert resp_search.status_code == 200, f"Failed product search: {resp_search.status_code}"
        search_json = resp_search.json()
        assert "data" in search_json and isinstance(search_json["data"], list), "Missing 'data' in search response"
        # verify returns product with code 'LST-P1-*'
        matching_products = [p for p in search_json["data"] if p.get("code") == search_term]
        assert len(matching_products) >= 1, f"No product with code {search_term} found in search results"

        # Step 4: GET /v1/products?page=1&pageSize=1 — verify pagination returns exactly 1 item
        params_pagination = {"page": 1, "pageSize": 1}
        resp_page1 = requests.get(f"{BASE_URL}{PRODUCTS_ENDPOINT}", headers=headers, params=params_pagination, timeout=TIMEOUT)
        assert resp_page1.status_code == 200, f"Failed pagination request: {resp_page1.status_code}"
        page1_json = resp_page1.json()
        assert "data" in page1_json and isinstance(page1_json["data"], list), "Missing 'data' in pagination response"
        assert len(page1_json["data"]) == 1, f"Expected exactly 1 product in pagination but got {len(page1_json['data'])}"
        assert "pagination" in page1_json and isinstance(page1_json["pagination"], dict), "Missing 'pagination' in response"

    finally:
        # Cleanup: Delete created products
        headers = {"Authorization": f"Bearer {access_token}"} if 'access_token' in locals() else {}
        for pid in locals().get("created_product_ids", []):
            try:
                requests.delete(f"{BASE_URL}{PRODUCTS_ENDPOINT}/{pid}", headers=headers, timeout=TIMEOUT)
            except Exception:
                pass

test_products_list_with_search_and_filters()