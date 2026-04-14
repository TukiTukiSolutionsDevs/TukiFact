import requests

BASE_URL = "http://localhost:80"
LOGIN_ENDPOINT = "/v1/auth/login"
RETENTIONS_ENDPOINT = "/v1/retentions"

LOGIN_PAYLOAD = {
    "email": "prdtest@test.pe",
    "password": "PrdTest2026!",
    "tenantId": "b2ba4cad-45c9-4a98-96f9-bf2eb6c96572"
}

# ID from TC057 test case (retention_create_tipo_20_tasa_3pct)
RETENTION_ID_TC057 = None

def test_retention_get_by_id_with_references():
    global RETENTION_ID_TC057

    try:
        # Step 1: Authenticate and get access token
        login_response = requests.post(
            f"{BASE_URL}{LOGIN_ENDPOINT}",
            json=LOGIN_PAYLOAD,
            timeout=30
        )
        assert login_response.status_code == 200, f"Login failed with status {login_response.status_code}"
        login_json = login_response.json()
        access_token = login_json.get("accessToken")
        assert access_token and access_token.startswith("eyJ"), "Invalid accessToken received"

        headers = {
            "Authorization": f"Bearer {access_token}"
        }

        # If RETENTION_ID_TC057 is not set, create retention as per TC057

        if RETENTION_ID_TC057 is None:
            retention_create_payload = {
                "serie": "R001",
                "supplierDocType": "6",
                "supplierDocNumber": "20100070970",
                "supplierName": "Proveedor Retenido SAC",
                "supplierAddress": "Av Proveedor 456",
                "regimeCode": "01",
                "retentionPercent": 3.00,
                "currency": "PEN",
                "notes": "Retención de prueba PRD",
                "references": [
                    {
                        "documentType": "01",
                        "documentNumber": "F001-00000100",
                        "documentDate": "2026-04-01",
                        "invoiceAmount": 5000.00,
                        "invoiceCurrency": "PEN",
                        "paymentDate": "2026-04-10",
                        "paymentNumber": 1,
                        "paymentAmount": 5000.00
                    },
                    {
                        "documentType": "01",
                        "documentNumber": "F001-00000101",
                        "documentDate": "2026-04-02",
                        "invoiceAmount": 3000.00,
                        "invoiceCurrency": "PEN",
                        "paymentDate": "2026-04-10",
                        "paymentNumber": 1,
                        "paymentAmount": 3000.00
                    }
                ]
            }
            create_resp = requests.post(
                f"{BASE_URL}{RETENTIONS_ENDPOINT}",
                json=retention_create_payload,
                headers=headers,
                timeout=30
            )
            assert create_resp.status_code == 201, f"Retention creation failed with status {create_resp.status_code}"
            create_json = create_resp.json()
            retention_id = create_json.get("id")
            assert retention_id, "No retention ID in creation response"
            RETENTION_ID_TC057 = retention_id
        else:
            retention_id = RETENTION_ID_TC057

        # Step 2: GET retention by ID
        get_resp = requests.get(
            f"{BASE_URL}{RETENTIONS_ENDPOINT}/{retention_id}",
            headers=headers,
            timeout=30
        )
        assert get_resp.status_code == 200, f"Get retention failed with status {get_resp.status_code}"
        retention_data = get_resp.json()

        # Validate main fields
        full_number = retention_data.get("fullNumber")
        supplier_name = retention_data.get("supplierName")
        references = retention_data.get("references")

        assert full_number and full_number.startswith("R001-"), "Retention fullNumber missing or invalid prefix"
        assert supplier_name == "Proveedor Retenido SAC", f"SupplierName mismatch: expected 'Proveedor Retenido SAC', got '{supplier_name}'"
        assert isinstance(references, list), "'references' field must be a list"
        assert len(references) == 2, f"Expected 2 references but got {len(references)}"

        for ref in references:
            assert "documentNumber" in ref and ref["documentNumber"], "Reference missing 'documentNumber'"
            assert "invoiceAmount" in ref and isinstance(ref["invoiceAmount"], (int,float)), "Reference missing or invalid 'invoiceAmount'"
            assert "paymentAmount" in ref and isinstance(ref["paymentAmount"], (int,float)), "Reference missing or invalid 'paymentAmount'"
            assert "retainedAmount" in ref and isinstance(ref["retainedAmount"], (int,float)), "Reference missing or invalid 'retainedAmount'"
            assert "netPaidAmount" in ref and isinstance(ref["netPaidAmount"], (int,float)), "Reference missing or invalid 'netPaidAmount'"

    finally:
        if RETENTION_ID_TC057:
            # Cleanup - delete the created retention
            # Assuming DELETE /v1/retentions/{id} is supported for cleanup (not specified in PRD)
            # If deletion is not supported, just pass
            try:
                requests.delete(
                    f"{BASE_URL}{RETENTIONS_ENDPOINT}/{RETENTION_ID_TC057}",
                    headers=headers,
                    timeout=30
                )
            except Exception:
                pass

test_retention_get_by_id_with_references()