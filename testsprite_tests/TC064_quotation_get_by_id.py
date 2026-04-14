import requests
import time


def test_quotation_get_by_id():
    base_url = "http://localhost:80"
    auth_url = f"{base_url}/v1/auth/login"
    quotations_url = f"{base_url}/v1/quotations"

    login_payload = {
        "email": "prdtest@test.pe",
        "password": "PrdTest2026!",
        "tenantId": "b2ba4cad-45c9-4a98-96f9-bf2eb6c96572",
    }
    timeout = 30

    # Step 1: Login to get access token
    try:
        auth_resp = requests.post(auth_url, json=login_payload, timeout=timeout)
        auth_resp.raise_for_status()
    except Exception as e:
        assert False, f"Login request failed: {e}"

    auth_json = auth_resp.json()
    access_token = auth_json.get("accessToken")
    assert access_token and isinstance(access_token, str) and access_token.startswith("eyJ"), "Invalid accessToken"
    headers = {"Authorization": f"Bearer {access_token}"}

    # Step 2: Create a new quotation to get an ID
    quotation_creation_payload = {
        "customerDocType": "6",
        "customerDocNumber": "20100070970",
        "customerName": "Cotización SAC",
        "customerAddress": "Av Cotización 100",
        "customerEmail": "cotizacion@test.pe",
        "customerPhone": "01-1111111",
        "currency": "PEN",
        "validUntil": "2026-05-15",
        "notes": "Cotización de prueba",
        "termsAndConditions": "Pago a 30 días",
        "items": [
            {
                "productCode": "PROD-001",
                "description": "Laptop HP",
                "quantity": 5,
                "unitMeasure": "NIU",
                "unitPrice": 2542.37,
                "igvType": "10",
                "discount": 0,
            },
            {
                "productCode": "SERV-001",
                "description": "Instalación y configuración",
                "quantity": 5,
                "unitMeasure": "ZZ",
                "unitPrice": 200.00,
                "igvType": "10",
                "discount": 100.00,
            },
        ],
    }

    quotation_id = None
    try:
        create_resp = requests.post(quotations_url, json=quotation_creation_payload, headers=headers, timeout=timeout)
        create_resp.raise_for_status()
        create_json = create_resp.json()
        quotation_id = create_json.get("id")
        assert quotation_id, "No quotation ID returned on creation"
    except Exception as e:
        assert False, f"Quotation creation failed: {e}"

    if quotation_id is None:
        assert False, "Quotation ID not obtained; cannot proceed with get by id test"

    # Step 3: Get quotation by ID
    try:
        get_resp = requests.get(f"{quotations_url}/{quotation_id}", headers=headers, timeout=timeout)
        get_resp.raise_for_status()
    except Exception as e:
        assert False, f"Get quotation by ID failed: {e}"

    data = get_resp.json()

    # Validate response fields and values
    assert "quotationNumber" in data and isinstance(data["quotationNumber"], str) and data["quotationNumber"].startswith("COT-"), "Invalid or missing quotationNumber"
    assert data.get("customerName") == "Cotización SAC", f"customerName is '{data.get('customerName')}', expected 'Cotización SAC'"
    items = data.get("items")
    assert isinstance(items, list) and len(items) == 2, f"items array length is {len(items) if items else 'None'}, expected 2"
    assert "validUntil" in data and isinstance(data["validUntil"], str) and data["validUntil"], "Missing or invalid validUntil"
    assert "notes" in data and isinstance(data["notes"], str), "Missing or invalid notes"
    assert data.get("status") == "draft", f"status is '{data.get('status')}', expected 'draft'"

    # Cleanup: delete created quotation
    try:
        del_resp = requests.delete(f"{quotations_url}/{quotation_id}", headers=headers, timeout=timeout)
        if del_resp.status_code not in [200, 204, 404]:
            # 404 means already deleted or not found, which is acceptable
            assert False, f"Deleting test quotation failed with status {del_resp.status_code}"
    except Exception as e:
        # Log but do not fail test on cleanup failure
        pass


test_quotation_get_by_id()