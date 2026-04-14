import requests

BASE_URL = "http://localhost:80"
BACKOFFICE_LOGIN_ENDPOINT = "/v1/backoffice/auth/login"
BACKOFFICE_EMPLOYEES_ENDPOINT = "/v1/backoffice/employees"
TIMEOUT = 30

BACKOFFICE_CREDENTIALS = {
    "email": "superadmin@tukifact.net.pe",
    "password": "SuperAdmin2026!"
}

def test_backoffice_employees_invalid_role_returns_400():
    try:
        # Authenticate to get BACKOFFICE_TOKEN
        login_resp = requests.post(
            BASE_URL + BACKOFFICE_LOGIN_ENDPOINT,
            json=BACKOFFICE_CREDENTIALS,
            timeout=TIMEOUT
        )
        assert login_resp.status_code == 200, f"Login failed with status {login_resp.status_code}"
        login_json = login_resp.json()
        access_token = login_json.get("accessToken")
        assert isinstance(access_token, str) and access_token.startswith("eyJ"), "Invalid or missing accessToken"

        headers = {
            "Authorization": f"Bearer {access_token}",
            "Content-Type": "application/json"
        }

        # Prepare invalid role employee data
        employee_data = {
            "email": "bad@tukifact.net.pe",
            "fullName": "Bad",
            "password": "Bad2026!",
            "role": "admin"  # invalid role, valid are superadmin, support, ops, billing
        }

        # Attempt to create employee with invalid role
        resp = requests.post(
            BASE_URL + BACKOFFICE_EMPLOYEES_ENDPOINT,
            json=employee_data,
            headers=headers,
            timeout=TIMEOUT
        )

        # Validate 400 Bad Request
        assert resp.status_code == 400, f"Expected 400 but got {resp.status_code}"
        resp_json = resp.json()
        error_msg = str(resp_json.get("error") or resp_json.get("message") or "")
        # Confirm error message mentions invalid role or admin not allowed
        assert any(keyword in error_msg.lower() for keyword in ["role", "admin", "invalid"]), \
            f"Error message does not indicate invalid role: {error_msg}"

    except requests.RequestException as e:
        assert False, f"Request failed: {str(e)}"

test_backoffice_employees_invalid_role_returns_400()