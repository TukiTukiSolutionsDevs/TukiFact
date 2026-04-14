import requests
import time

BASE_URL = "http://localhost:80"
LOGIN_ENDPOINT = "/v1/auth/login"
CUSTOMERS_ENDPOINT = "/v1/customers"

def test_customers_list_with_search_and_filters():
    # Step 0: Login to get token
    login_payload = {
        "email": "prdtest@test.pe",
        "password": "PrdTest2026!",
        "tenantId": "b2ba4cad-45c9-4a98-96f9-bf2eb6c96572"
    }
    try:
        login_response = requests.post(
            BASE_URL + LOGIN_ENDPOINT,
            json=login_payload,
            timeout=30
        )
        login_response.raise_for_status()
    except Exception as e:
        assert False, f"Login failed: {e}"
    login_data = login_response.json()
    access_token = login_data.get("accessToken")
    assert isinstance(access_token, str) and access_token.startswith("eyJ"), "Invalid accessToken in login response"

    headers = {"Authorization": f"Bearer {access_token}"}

    created_customer_ids = []
    try:
        # Step 1: Create 2 customers first
        customer1_payload = {
            "docType": "1",
            "docNumber": "71234501",
            "name": "DNI Customer Test 1",
            "email": "dni1@test.pe"
        }
        resp1 = requests.post(
            BASE_URL + CUSTOMERS_ENDPOINT,
            json=customer1_payload,
            headers=headers,
            timeout=30
        )
        if resp1.status_code == 409:
            # If duplicate, we continue, but must verify later that at least one exists
            pass
        else:
            resp1.raise_for_status()
            data1 = resp1.json()
            cid1 = data1.get("id")
            assert cid1, "Customer 1 creation response missing 'id'"
            created_customer_ids.append(cid1)

        customer2_payload = {
            "docType": "6",
            "docNumber": "20100000501",
            "name": "RUC Customer Test 1",
            "email": "ruc1@test.pe"
        }
        resp2 = requests.post(
            BASE_URL + CUSTOMERS_ENDPOINT,
            json=customer2_payload,
            headers=headers,
            timeout=30
        )
        if resp2.status_code == 409:
            pass
        else:
            resp2.raise_for_status()
            data2 = resp2.json()
            cid2 = data2.get("id")
            assert cid2, "Customer 2 creation response missing 'id'"
            created_customer_ids.append(cid2)

        # Step 2: GET /v1/customers?page=1&pageSize=10
        params_step2 = {"page": 1, "pageSize": 10}
        resp_list = requests.get(
            BASE_URL + CUSTOMERS_ENDPOINT,
            headers=headers,
            params=params_step2,
            timeout=30
        )
        resp_list.raise_for_status()
        list_data = resp_list.json()
        assert "data" in list_data and isinstance(list_data["data"], list), "'data' array missing or invalid"
        assert len(list_data["data"]) >= 2, "Less than 2 customers in list"
        assert "pagination" in list_data and isinstance(list_data["pagination"], dict), "'pagination' missing or invalid"

        # Step 3: GET /v1/customers?docType=1
        params_docType = {"docType": "1"}
        resp_docType = requests.get(
            BASE_URL + CUSTOMERS_ENDPOINT,
            headers=headers,
            params=params_docType,
            timeout=30
        )
        resp_docType.raise_for_status()
        docType_data = resp_docType.json()
        assert "data" in docType_data and isinstance(docType_data["data"], list), "'data' missing or invalid in docType filter"
        filtered_customers = [c for c in docType_data["data"] if c.get("docType") == "1"]
        assert len(filtered_customers) >= 1, "No customers found with docType=1"

        # Step 4: GET /v1/customers?search=DNI+Customer
        params_search = {"search": "DNI Customer"}
        resp_search = requests.get(
            BASE_URL + CUSTOMERS_ENDPOINT,
            headers=headers,
            params=params_search,
            timeout=30
        )
        resp_search.raise_for_status()
        search_data = resp_search.json()
        assert "data" in search_data and isinstance(search_data["data"], list), "'data' missing or invalid in search"
        assert len(search_data["data"]) >= 1, "No customers found with search 'DNI Customer'"

    finally:
        # Clean up created customers
        for cid in created_customer_ids:
            try:
                del_resp = requests.delete(
                    f"{BASE_URL}{CUSTOMERS_ENDPOINT}/{cid}",
                    headers=headers,
                    timeout=30
                )
                if del_resp.status_code not in [200, 204, 404]:
                    # Accept 404 since resource might have been deleted already
                    del_resp.raise_for_status()
            except Exception:
                pass


test_customers_list_with_search_and_filters()