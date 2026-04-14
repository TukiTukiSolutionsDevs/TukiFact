import requests
import time
import uuid

BASE_URL = "http://localhost:80"
LOGIN_ENDPOINT = "/v1/auth/login"
DESPATCH_ADVICES_ENDPOINT = "/v1/despatch-advices"
TIMEOUT = 30

def test_despatch_advice_create_draft_gre_remitente():
    # Step 1: Authentication
    login_payload = {
        "email": "prdtest@test.pe",
        "password": "PrdTest2026!",
        "tenantId": "b2ba4cad-45c9-4a98-96f9-bf2eb6c96572"
    }
    try:
        login_resp = requests.post(
            BASE_URL + LOGIN_ENDPOINT,
            json=login_payload,
            timeout=TIMEOUT
        )
        login_resp.raise_for_status()
    except requests.RequestException as e:
        assert False, f"Authentication request failed: {e}"

    login_json = login_resp.json()
    access_token = login_json.get("accessToken")
    assert access_token and isinstance(access_token, str) and access_token.startswith("eyJ"), \
        "accessToken missing or invalid in login response"

    headers = {
        "Authorization": f"Bearer {access_token}",
        "Content-Type": "application/json"
    }

    # Step 2: Prepare despatch advice payload
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
                "productCode": "ELEC-001",
                "quantity": 10,
                "unitCode": "NIU"
            },
            {
                "description": "Pallet de alimentos",
                "productCode": "FOOD-002",
                "quantity": 5,
                "unitCode": "KGM"
            }
        ]
    }

    # Step 3: POST despatch advice
    try:
        response = requests.post(
            BASE_URL + DESPATCH_ADVICES_ENDPOINT,
            json=despatch_payload,
            headers=headers,
            timeout=TIMEOUT
        )
    except requests.RequestException as e:
        assert False, f"POST /v1/despatch-advices request failed: {e}"

    assert response.status_code == 201, f"Expected 201 Created, got {response.status_code}"

    response_json = response.json()
    despatch_id = response_json.get("id")
    full_number = response_json.get("fullNumber")
    status = response_json.get("status")

    # Validate id is UUID
    try:
        uuid_obj = uuid.UUID(despatch_id)
    except Exception:
        assert False, f"Returned id is not a valid UUID: {despatch_id}"

    assert full_number and full_number.startswith("T001-"), \
        f"fullNumber missing or does not start with 'T001-': {full_number}"
    assert status and isinstance(status, str), "status missing or not a string"

    # Save despatch_id as global for TC034 usage if desired
    global GRE_ID_TC033
    GRE_ID_TC033 = despatch_id


test_despatch_advice_create_draft_gre_remitente()