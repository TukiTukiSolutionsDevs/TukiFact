import requests
import time

BASE_URL = "http://localhost:80"
LOGIN_ENDPOINT = "/v1/auth/login"
PERCEPTIONS_ENDPOINT = "/v1/perceptions"
LOGIN_PAYLOAD = {
    "email": "prdtest@test.pe",
    "password": "PrdTest2026!",
    "tenantId": "b2ba4cad-45c9-4a98-96f9-bf2eb6c96572"
}
TIMEOUT = 30


def test_perception_create_tipo_40_tasa_2pct():
    token = None
    perception_id = None
    headers = None
    try:
        # Authenticate
        login_resp = requests.post(
            BASE_URL + LOGIN_ENDPOINT,
            json=LOGIN_PAYLOAD,
            timeout=TIMEOUT
        )
        assert login_resp.status_code == 200, f"Login failed with status {login_resp.status_code}"
        login_json = login_resp.json()
        assert "accessToken" in login_json and isinstance(login_json["accessToken"], str) and login_json["accessToken"].startswith("eyJ"), "AccessToken missing or invalid in login response"
        token = login_json["accessToken"]
        headers = {"Authorization": f"Bearer {token}", "Content-Type": "application/json"}

        perception_body = {
            "serie": "P001",
            "customerDocType": "6",
            "customerDocNumber": "20100070970",
            "customerName": "Cliente Percibido SAC",
            "customerAddress": "Av Cliente 789",
            "regimeCode": "01",
            "perceptionPercent": 2.00,
            "currency": "PEN",
            "notes": "Percepción de prueba PRD",
            "references": [
                {
                    "documentType": "01",
                    "documentNumber": "F001-00000200",
                    "documentDate": "2026-04-01",
                    "invoiceAmount": 10000.00,
                    "invoiceCurrency": "PEN",
                    "collectionDate": "2026-04-12",
                    "collectionNumber": 1,
                    "collectionAmount": 10000.00
                }
            ]
        }

        # Create perception
        perception_resp = requests.post(
            BASE_URL + PERCEPTIONS_ENDPOINT,
            headers=headers,
            json=perception_body,
            timeout=TIMEOUT
        )
        assert perception_resp.status_code == 201, f"Perception creation failed with status {perception_resp.status_code}"
        p_json = perception_resp.json()

        # Validate response fields
        assert "id" in p_json and isinstance(p_json["id"], str) and len(p_json["id"]) > 0, "Missing or invalid 'id' in perception"
        perception_id = p_json["id"]
        assert "fullNumber" in p_json and p_json["fullNumber"].startswith("P001-"), "'fullNumber' not starting with 'P001-'"
        assert abs(float(p_json.get("perceptionPercent", 0)) - 2.00) < 1e-6, f"perceptionPercent expected 2.00, got {p_json.get('perceptionPercent')}"
        assert abs(float(p_json.get("totalInvoiceAmount", 0)) - 10000.00) < 1e-6, f"totalInvoiceAmount expected 10000.00, got {p_json.get('totalInvoiceAmount')}"
        assert abs(float(p_json.get("totalPerceived", 0)) - 200.00) < 1e-6, f"totalPerceived expected 200.00, got {p_json.get('totalPerceived')}"
        assert abs(float(p_json.get("totalCollected", 0)) - 10200.00) < 1e-6, f"totalCollected expected 10200.00, got {p_json.get('totalCollected')}"

        assert "references" in p_json and isinstance(p_json["references"], list) and len(p_json["references"]) == 1, "References array missing or does not have exactly 1 element"
    finally:
        # Clean up - delete perception if created
        if token and perception_id:
            try:
                del_resp = requests.delete(
                    f"{BASE_URL}{PERCEPTIONS_ENDPOINT}/{perception_id}",
                    headers={"Authorization": f"Bearer {token}"}
                )
                # Accept 200, 204, or 404 in case already deleted
                assert del_resp.status_code in [200, 204, 404], f"Unexpected status code on perception delete: {del_resp.status_code}"
            except Exception:
                pass


test_perception_create_tipo_40_tasa_2pct()