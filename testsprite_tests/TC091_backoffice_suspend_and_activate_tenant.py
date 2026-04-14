import requests
import time

BASE_URL = "http://localhost:80"
REGISTER_URL = f"{BASE_URL}/v1/auth/register"
BACKOFFICE_LOGIN_URL = f"{BASE_URL}/v1/backoffice/auth/login"
BACKOFFICE_SUSPEND_URL_TEMPLATE = f"{BASE_URL}/v1/backoffice/tenants/{{tenantId}}/suspend"
BACKOFFICE_ACTIVATE_URL_TEMPLATE = f"{BASE_URL}/v1/backoffice/tenants/{{tenantId}}/activate"

BACKOFFICE_EMAIL = "superadmin@tukifact.net.pe"
BACKOFFICE_PASSWORD = "SuperAdmin2026!"

def test_backoffice_suspend_and_activate_tenant():
    timeout = 30

    # Step 1: Backoffice login to get BACKOFFICE_TOKEN
    try:
        login_resp = requests.post(
            BACKOFFICE_LOGIN_URL,
            json={"email": BACKOFFICE_EMAIL, "password": BACKOFFICE_PASSWORD},
            timeout=timeout
        )
        assert login_resp.status_code == 200, f"Backoffice login failed with status {login_resp.status_code}"
        login_data = login_resp.json()
        assert "accessToken" in login_data and login_data["accessToken"].startswith("eyJ"), "Invalid backoffice accessToken"
        assert "user" in login_data and login_data["user"].get("role") == "superadmin", "Backoffice user role is not superadmin"
        backoffice_token = login_data["accessToken"]
    except Exception as e:
        raise AssertionError(f"Backoffice login request failed: {e}")

    headers = {"Authorization": f"Bearer {backoffice_token}"}

    # Generate unique RUC and email to avoid conflict
    timestamp = int(time.time())
    unique_ruc_suffix = str(timestamp)[-8:]  # last 8 digits of timestamp for uniqueness
    # Generating a 11-digit RUC starting with '20' + suffix padded if necessary
    unique_ruc = f"20{unique_ruc_suffix.zfill(9)}"  
    unique_email = f"suspend{timestamp}@test.pe"

    # Step 2: Register a throwaway tenant
    register_payload = {
        "ruc": unique_ruc,
        "razonSocial": "Suspend Test SAC",
        "adminEmail": unique_email,
        "adminPassword": "Suspend2026!",
        "adminFullName": "Suspend User"
    }
    tenant_id = None

    try:
        register_resp = requests.post(
            REGISTER_URL,
            json=register_payload,
            timeout=timeout
        )
        assert register_resp.status_code == 201, f"Tenant registration failed: {register_resp.status_code} {register_resp.text}"
        reg_data = register_resp.json()
        # Expect response to contain tenantId somewhere, Assuming tenantId is top-level or inside user object
        if "tenantId" in reg_data:
            tenant_id = reg_data["tenantId"]
        elif "user" in reg_data and "tenantId" in reg_data["user"]:
            tenant_id = reg_data["user"]["tenantId"]
        else:
            # Sometimes might be inside accessToken claims but not accessible here. Fail if not found.
            # Attempt to infer from response - fallback fail.
            raise AssertionError("tenantId not found in registration response")
        assert isinstance(tenant_id, str) and len(tenant_id) > 0, "Invalid tenantId received"
        
        # Step 3: Put suspend tenant
        suspend_url = BACKOFFICE_SUSPEND_URL_TEMPLATE.format(tenantId=tenant_id)
        suspend_resp = requests.put(suspend_url, headers=headers, timeout=timeout)
        assert suspend_resp.status_code == 200, f"Suspend request failed: {suspend_resp.status_code} {suspend_resp.text}"
        suspend_msg = suspend_resp.json().get("message", "").lower()
        assert "suspendido" in suspend_msg, f"Suspend message missing expected text: {suspend_msg}"

        # Step 4: Put activate tenant
        activate_url = BACKOFFICE_ACTIVATE_URL_TEMPLATE.format(tenantId=tenant_id)
        activate_resp = requests.put(activate_url, headers=headers, timeout=timeout)
        assert activate_resp.status_code == 200, f"Activate request failed: {activate_resp.status_code} {activate_resp.text}"
        activate_msg = activate_resp.json().get("message", "").lower()
        assert "activado" in activate_msg, f"Activate message missing expected text: {activate_msg}"

    finally:
        # Cleanup: Delete the tenant to avoid clutter - assume backoffice API supports DELETE /v1/backoffice/tenants/{id}
        # If no delete method documented, ignore cleanup.
        # The PRD did not mention delete tenant endpoint, so skip cleanup.
        pass

test_backoffice_suspend_and_activate_tenant()