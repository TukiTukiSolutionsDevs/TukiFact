import requests

BASE_URL = "http://localhost:80"
LOGIN_URL = f"{BASE_URL}/v1/auth/login"
QUOTATIONS_URL = f"{BASE_URL}/v1/quotations"
TIMEOUT = 30

def test_quotation_create_with_items():
    login_payload = {
        "email": "prdtest@test.pe",
        "password": "PrdTest2026!",
        "tenantId": "b2ba4cad-45c9-4a98-96f9-bf2eb6c96572"
    }

    # Authenticate and get token
    try:
        login_resp = requests.post(LOGIN_URL, json=login_payload, timeout=TIMEOUT)
        login_resp.raise_for_status()
        login_data = login_resp.json()
        access_token = login_data.get("accessToken")
        assert access_token and access_token.startswith("eyJ"), "Invalid access token"

        headers = {
            "Authorization": f"Bearer {access_token}",
            "Content-Type": "application/json"
        }

        quotation_payload = {
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
                    "discount": 0
                },
                {
                    "productCode": "SERV-001",
                    "description": "Instalación y configuración",
                    "quantity": 5,
                    "unitMeasure": "ZZ",
                    "unitPrice": 200.00,
                    "igvType": "10",
                    "discount": 100.00
                }
            ]
        }

        resp = requests.post(QUOTATIONS_URL, json=quotation_payload, headers=headers, timeout=TIMEOUT)
        assert resp.status_code == 201, f"Expected 201 Created, got {resp.status_code}"
        data = resp.json()

        # Validate fields in response
        quotation_id = data.get("id")
        assert quotation_id, "Response missing 'id'"
        quotation_number = data.get("quotationNumber")
        assert quotation_number and quotation_number.startswith("COT-"), "Invalid or missing 'quotationNumber'"
        status = data.get("status")
        assert status == "draft", f"Expected status 'draft', got '{status}'"
        subtotal = data.get("subtotal")
        igv = data.get("igv")
        total = data.get("total")
        assert isinstance(subtotal, (int, float)) and subtotal > 0, "'subtotal' must be a positive number"
        assert isinstance(igv, (int, float)) and igv > 0, "'igv' must be a positive number"
        assert isinstance(total, (int, float)) and total > 0, "'total' must be a positive number"
        items = data.get("items")
        assert isinstance(items, list) and len(items) == 2, "There must be exactly 2 items"

    except requests.RequestException as e:
        assert False, f"Request failed: {str(e)}"


test_quotation_create_with_items()