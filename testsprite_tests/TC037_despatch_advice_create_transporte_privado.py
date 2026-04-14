import requests
import time

BASE_URL = "http://localhost:80"
LOGIN_URL = f"{BASE_URL}/v1/auth/login"
DESPATCH_ADVICES_URL = f"{BASE_URL}/v1/despatch-advices"
TIMEOUT = 30

def test_despatch_advice_create_transporte_privado():
    login_payload = {
        "email": "prdtest@test.pe",
        "password": "PrdTest2026!",
        "tenantId": "b2ba4cad-45c9-4a98-96f9-bf2eb6c96572"
    }
    try:
        # Authenticate and get access token
        login_resp = requests.post(LOGIN_URL, json=login_payload, timeout=TIMEOUT)
        assert login_resp.status_code == 200, f"Login failed: {login_resp.status_code}, {login_resp.text}"
        login_json = login_resp.json()
        access_token = login_json.get("accessToken")
        assert access_token and isinstance(access_token, str) and access_token.startswith("eyJ"), "Invalid accessToken"

        headers = {
            "Authorization": f"Bearer {access_token}",
            "Content-Type": "application/json"
        }

        # Prepare despatch advice creation payload
        despatch_payload = {
            "documentType": "09",
            "serie": "T001",
            "transferStartDate": "2026-04-16",
            "transferReasonCode": "04",
            "transferReasonDescription": "Traslado entre establecimientos",
            "grossWeight": 50.0,
            "weightUnitCode": "KGM",
            "totalPackages": 1,
            "transportMode": "02",
            "driverDocType": "1",
            "driverDocNumber": "71234567",
            "driverName": "Carlos Pérez",
            "driverLicense": "Q71234567",
            "vehiclePlate": "ABC-123",
            "recipientDocType": "6",
            "recipientDocNumber": "20100070970",
            "recipientName": "Sucursal SAC",
            "originUbigeo": "150101",
            "originAddress": "Local Principal Lima",
            "destinationUbigeo": "040101",
            "destinationAddress": "Sucursal Arequipa",
            "items": [
                {
                    "description": "Mercadería interna",
                    "quantity": 20,
                    "unitCode": "NIU"
                }
            ]
        }

        # Create despatch advice
        resp = requests.post(DESPATCH_ADVICES_URL, headers=headers, json=despatch_payload, timeout=TIMEOUT)
        assert resp.status_code == 201, f"Expected 201 Created, got {resp.status_code}. Response: {resp.text}"

        data = resp.json()
        # Check transportMode reflected
        assert data.get("transportMode") == despatch_payload["transportMode"], "transportMode not reflected correctly"

    except requests.RequestException as e:
        assert False, f"Request failed: {e}"

test_despatch_advice_create_transporte_privado()
