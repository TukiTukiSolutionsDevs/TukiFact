import requests

BASE_URL = "http://localhost:80"
LOGIN_URL = f"{BASE_URL}/v1/auth/login"
PERCEPTION_ID_TC060 = None  # Will be set after creating perception in TC060

def test_perception_get_by_id_with_references():
    # Step 1: Authenticate to get Bearer token
    login_payload = {
        "email": "prdtest@test.pe",
        "password": "PrdTest2026!",
        "tenantId": "b2ba4cad-45c9-4a98-96f9-bf2eb6c96572"
    }
    try:
        login_resp = requests.post(LOGIN_URL, json=login_payload, timeout=30)
        login_resp.raise_for_status()
    except requests.RequestException as e:
        assert False, f"Login request failed: {e}"
    login_json = login_resp.json()
    access_token = login_json.get("accessToken")
    assert access_token and access_token.startswith("eyJ"), "Invalid accessToken in login response"

    headers = {
        "Authorization": f"Bearer {access_token}"
    }

    # Step 2: If PERCEPTION_ID_TC060 not set, create perception as in TC060, then use its ID
    global PERCEPTION_ID_TC060
    if not PERCEPTION_ID_TC060:
        perception_create_payload = {
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

        perception_create_url = f"{BASE_URL}/v1/perceptions"
        try:
            create_resp = requests.post(perception_create_url, json=perception_create_payload, headers=headers, timeout=30)
            create_resp.raise_for_status()
        except requests.RequestException as e:
            assert False, f"Perception creation failed: {e}"

        create_json = create_resp.json()
        perception_id = create_json.get("id")
        assert perception_id, "Perception ID not found in create response"
        PERCEPTION_ID_TC060 = perception_id
    else:
        perception_id = PERCEPTION_ID_TC060

    # Step 3: Get perception by ID
    perception_get_url = f"{BASE_URL}/v1/perceptions/{perception_id}"
    try:
        get_resp = requests.get(perception_get_url, headers=headers, timeout=30)
        get_resp.raise_for_status()
    except requests.RequestException as e:
        assert False, f"Perception GET by ID failed: {e}"

    get_json = get_resp.json()
    # Validations as per TC061
    assert "fullNumber" in get_json and isinstance(get_json["fullNumber"], str) and get_json["fullNumber"].startswith("P001-"), \
        "fullNumber missing or invalid"
    assert get_json.get("customerName") == "Cliente Percibido SAC", "customerName does not match expected value"

    references = get_json.get("references")
    assert isinstance(references, list) and len(references) == 1, "References array missing or not length 1"
    ref = references[0]
    perceived_amount = ref.get("perceivedAmount")
    total_collected_amount = ref.get("totalCollectedAmount")
    assert perceived_amount == 200.00, f"Expected perceivedAmount 200.00 but got {perceived_amount}"
    assert total_collected_amount == 10200.00, f"Expected totalCollectedAmount 10200.00 but got {total_collected_amount}"

test_perception_get_by_id_with_references()