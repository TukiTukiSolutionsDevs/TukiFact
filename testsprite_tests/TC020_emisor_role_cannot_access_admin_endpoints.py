import requests
import uuid

BASE_URL = "http://localhost:5186"
TIMEOUT = 30

def test_emisor_role_cannot_access_admin_endpoints():
    # Step 1: Login as admin to get admin token
    admin_login_payload = {
        "email": "admin@tukitest.pe",
        "password": "TestSprite2026!",
        "tenantId": "0c700fd7-1fdb-43d5-a2a8-d20fe4bcab84"
    }
    resp = requests.post(f"{BASE_URL}/v1/auth/login", json=admin_login_payload, timeout=TIMEOUT)
    assert resp.status_code == 200, f"Admin login failed: {resp.text}"
    admin_access_token = resp.json().get("accessToken")
    assert admin_access_token and isinstance(admin_access_token, str), "Admin accessToken missing or invalid"

    admin_headers = {
        "Authorization": f"Bearer {admin_access_token}",
        "Content-Type": "application/json"
    }

    # Step 2: Create emisor user with unique email to avoid conflicts
    unique_suffix = uuid.uuid4().hex
    emisor_email = f"emisortest{unique_suffix}@test.pe"
    emisor_password = "Emisor2026!"
    emisor_body = {
        "email": emisor_email,
        "password": emisor_password,
        "fullName": "Emisor",
        "role": "emisor"
    }

    create_user_resp = requests.post(f"{BASE_URL}/v1/users", headers=admin_headers, json=emisor_body, timeout=TIMEOUT)
    assert create_user_resp.status_code == 201, f"Failed to create emisor user: {create_user_resp.text}"
    emisor_user = create_user_resp.json()
    emisor_user_id = emisor_user.get("id")
    assert emisor_user_id is not None, "Created emisor user ID missing"

    try:
        # Step 3: Login as emisor user
        emisor_login_payload = {
            "email": emisor_email,
            "password": emisor_password,
            "tenantId": "0c700fd7-1fdb-43d5-a2a8-d20fe4bcab84"
        }
        emisor_login_resp = requests.post(f"{BASE_URL}/v1/auth/login", json=emisor_login_payload, timeout=TIMEOUT)
        assert emisor_login_resp.status_code == 200, f"Emisor login failed: {emisor_login_resp.text}"
        emisor_access_token = emisor_login_resp.json().get("accessToken")
        assert emisor_access_token and isinstance(emisor_access_token, str), "Emisor accessToken missing or invalid"

        emisor_headers = {
            "Authorization": f"Bearer {emisor_access_token}",
            "Content-Type": "application/json"
        }

        # Step 4: Using emisor token, GET /v1/users should return 403 Forbidden
        users_resp = requests.get(f"{BASE_URL}/v1/users", headers=emisor_headers, timeout=TIMEOUT)
        assert users_resp.status_code == 403, f"Emisor GET /v1/users expected 403 but got {users_resp.status_code}"

        # Step 5: Using emisor token, POST /v1/series should return 403 Forbidden
        new_serie_payload = {
            "documentType": "01",
            "serie": "FXXX",
            "emissionPoint": "0001"
        }
        series_resp = requests.post(f"{BASE_URL}/v1/series", headers=emisor_headers, json=new_serie_payload, timeout=TIMEOUT)
        assert series_resp.status_code == 403, f"Emisor POST /v1/series expected 403 but got {series_resp.status_code}"

        # Step 6: Using emisor token, POST /v1/documents with a valid invoice body should return 201 Created
        # Use valid serie code per PRD specification
        valid_serie = "F001"
        doc_payload = {
            "documentType": "01",
            "serie": valid_serie,
            "currency": "PEN",
            "customerDocType": "6",
            "customerDocNumber": "20100070970",
            "customerName": "Cliente Emisor SAC",
            "items": [
                {
                    "description": "Servicio Emisor",
                    "quantity": 1,
                    "unitPrice": 100.00,
                    "unitMeasure": "ZZ",
                    "igvType": "10"
                }
            ]
        }

        doc_resp = requests.post(f"{BASE_URL}/v1/documents", headers=emisor_headers, json=doc_payload, timeout=TIMEOUT)
        assert doc_resp.status_code == 201, f"Emisor POST /v1/documents expected 201 but got {doc_resp.status_code}"
        doc_json = doc_resp.json()
        assert "id" in doc_json and isinstance(doc_json["id"], str), "Document ID missing or invalid"
        assert "fullNumber" in doc_json and doc_json["fullNumber"].startswith(valid_serie), "Document fullNumber invalid"
        assert "status" in doc_json, "Document status missing"
        assert "items" in doc_json and isinstance(doc_json["items"], list) and len(doc_json["items"]) == 1, "Document items missing or invalid"
    finally:
        # Cleanup: delete the emisor user
        delete_resp = requests.delete(f"{BASE_URL}/v1/users/{emisor_user_id}", headers=admin_headers, timeout=TIMEOUT)
        assert delete_resp.status_code == 204, f"Failed to delete emisor user in cleanup: {delete_resp.text}"

test_emisor_role_cannot_access_admin_endpoints()
