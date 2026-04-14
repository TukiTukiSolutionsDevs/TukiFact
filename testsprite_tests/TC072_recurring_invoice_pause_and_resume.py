import requests
import json

BASE_URL = "http://localhost:80"
LOGIN_ENDPOINT = "/v1/auth/login"
RECURRING_INVOICES_ENDPOINT = "/v1/recurring-invoices"

# Recurring ID from TC069 as per test instructions
RECURRING_INVOICE_ID = "220cebd5-53bf-406e-9171-6d891ee341d9"  # Provided in test plan ID for TC072 is same as TC069 ID


def test_recurring_invoice_pause_and_resume():
    login_payload = {
        "email": "prdtest@test.pe",
        "password": "PrdTest2026!",
        "tenantId": "b2ba4cad-45c9-4a98-96f9-bf2eb6c96572"
    }

    try:
        # Step 1: Authenticate and get access token
        login_resp = requests.post(
            BASE_URL + LOGIN_ENDPOINT,
            json=login_payload,
            timeout=30
        )
        assert login_resp.status_code == 200, f"Login failed: {login_resp.text}"
        login_data = login_resp.json()
        assert "accessToken" in login_data and isinstance(login_data["accessToken"], str) and login_data["accessToken"].startswith("eyJ"), "Invalid accessToken"
        access_token = login_data["accessToken"]

        headers = {
            "Authorization": f"Bearer {access_token}",
            "Content-Type": "application/json"
        }

        # Step 2: Pause the recurring invoice (PUT with status "paused")
        pause_payload = {"status": "paused"}
        pause_resp = requests.put(
            f"{BASE_URL}{RECURRING_INVOICES_ENDPOINT}/{RECURRING_INVOICE_ID}",
            headers=headers,
            json=pause_payload,
            timeout=30
        )
        assert pause_resp.status_code == 200, f"Pause request failed: {pause_resp.status_code} {pause_resp.text}"
        pause_data = pause_resp.json()
        assert "status" in pause_data and pause_data["status"] == "paused", f"Status not paused as expected: {pause_data.get('status')}"
        # 'nextEmissionDate' should be null (None in Python)
        assert "nextEmissionDate" in pause_data and pause_data["nextEmissionDate"] is None, f"nextEmissionDate expected to be null but was: {pause_data.get('nextEmissionDate')}"

        # Step 3: Resume the recurring invoice (PUT with status "active")
        resume_payload = {"status": "active"}
        resume_resp = requests.put(
            f"{BASE_URL}{RECURRING_INVOICES_ENDPOINT}/{RECURRING_INVOICE_ID}",
            headers=headers,
            json=resume_payload,
            timeout=30
        )
        assert resume_resp.status_code == 200, f"Resume request failed: {resume_resp.status_code} {resume_resp.text}"
        resume_data = resume_resp.json()
        assert "status" in resume_data and resume_data["status"] == "active", f"Status not active as expected: {resume_data.get('status')}"
        # 'nextEmissionDate' should be non-null
        assert "nextEmissionDate" in resume_data and resume_data["nextEmissionDate"] is not None, "nextEmissionDate expected to be non-null"

    except requests.RequestException as e:
        assert False, f"HTTP request failed: {e}"


test_recurring_invoice_pause_and_resume()