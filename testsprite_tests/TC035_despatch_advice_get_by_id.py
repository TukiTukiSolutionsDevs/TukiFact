import requests
import time

BASE_URL = "http://localhost:80"
LOGIN_URL = f"{BASE_URL}/v1/auth/login"
DESPATCH_ADVICES_URL = f"{BASE_URL}/v1/despatch-advices"

def test_despatch_advice_get_by_id():
    login_payload = {
        "email": "prdtest@test.pe",
        "password": "PrdTest2026!",
        "tenantId": "b2ba4cad-45c9-4a98-96f9-bf2eb6c96572"
    }
    try:
        # Step 1: Authenticate and get access token
        login_resp = requests.post(LOGIN_URL, json=login_payload, timeout=30)
        assert login_resp.status_code == 200, f"Login failed with status {login_resp.status_code}"
        login_json = login_resp.json()
        access_token = login_json.get("accessToken")
        assert access_token and isinstance(access_token, str) and access_token.startswith("eyJ"), "Invalid accessToken format"
        headers = {
            "Authorization": f"Bearer {access_token}"
        }

        # Step 2: Create new despatch advice (GRE) to get its ID for test independence
        timestamp = int(time.time())
        despatch_payload = {
            "documentType": "09",
            "serie": "T001",
            "transferStartDate": "2026-04-15",
            "transferReasonCode": "01",
            "transferReasonDescription": "Venta de mercadería",
            "grossWeight": 150.5,
            "weightUnitCode": "KGM",
            "totalPackages": 3,
            "transportMode": "01",
            "carrierDocType": "6",
            "carrierDocNumber": "20123456789",
            "carrierName": "Transportes Perú SAC",
            "carrierMtcNumber": "MTC-12345",
            "recipientDocType": "6",
            "recipientDocNumber": "20100070970",
            "recipientName": "Destinatario SAC",
            "originUbigeo": "150101",
            "originAddress": "Av. Origen 100, Lima",
            "destinationUbigeo": "040101",
            "destinationAddress": "Calle Destino 200, Arequipa",
            "relatedDocType": "01",
            "relatedDocNumber": "F001-00000001",
            "items": [
                {
                    "description": "Caja de productos electrónicos",
                    "productCode": f"ELEC-{timestamp}",
                    "quantity": 10,
                    "unitCode": "NIU"
                },
                {
                    "description": "Pallet de alimentos",
                    "productCode": f"FOOD-{timestamp}",
                    "quantity": 5,
                    "unitCode": "KGM"
                }
            ]
        }

        create_resp = requests.post(DESPATCH_ADVICES_URL, json=despatch_payload, headers=headers, timeout=30)
        assert create_resp.status_code == 201, f"Despatch advice creation failed with status {create_resp.status_code}"
        created_json = create_resp.json()
        despatch_id = created_json.get("id")
        full_number = created_json.get("fullNumber")
        assert despatch_id and isinstance(despatch_id, str), "Missing or invalid despatch advice ID"
        assert full_number and full_number.startswith("T001-"), "fullNumber does not start with 'T001-'"
        assert created_json.get("status") is not None, "Missing status in creation response"

        # Step 3: Get the despatch advice by ID
        get_url = f"{DESPATCH_ADVICES_URL}/{despatch_id}"
        get_resp = requests.get(get_url, headers=headers, timeout=30)
        assert get_resp.status_code == 200, f"GET despatch advice failed with status {get_resp.status_code}"
        data = get_resp.json()

        # Validate fields according to test case expectations
        assert isinstance(data, dict), "Response is not a JSON object"
        assert data.get("fullNumber", "").startswith("T001-"), "fullNumber does not start with 'T001-'"
        assert data.get("documentType") == "09", f"documentType expected '09', got '{data.get('documentType')}'"
        assert data.get("transferReasonCode") == "01", f"transferReasonCode expected '01', got '{data.get('transferReasonCode')}'"
        assert data.get("recipientName") == "Destinatario SAC", f"recipientName expected 'Destinatario SAC', got '{data.get('recipientName')}'"
        items = data.get("items")
        assert isinstance(items, list), "items is not a list"
        assert len(items) == 2, f"Expected 2 items, got {len(items)}"
        origin_address = data.get("originAddress")
        assert origin_address and isinstance(origin_address, str) and origin_address.strip() != "", "originAddress missing or empty"
        destination_address = data.get("destinationAddress")
        assert destination_address and isinstance(destination_address, str) and destination_address.strip() != "", "destinationAddress missing or empty"

    finally:
        # Clean up: Delete the created despatch advice to maintain test independence
        if 'despatch_id' in locals():
            del_resp = requests.delete(f"{DESPATCH_ADVICES_URL}/{despatch_id}", headers=headers, timeout=30)
            # No assertion on deletion status - best effort cleanup

test_despatch_advice_get_by_id()