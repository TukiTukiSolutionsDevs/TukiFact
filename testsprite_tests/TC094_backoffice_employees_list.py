import requests

BASE_URL = "http://localhost:80"
BACKOFFICE_AUTH_ENDPOINT = "/v1/backoffice/auth/login"
EMPLOYEES_ENDPOINT = "/v1/backoffice/employees"
BACKOFFICE_EMAIL = "superadmin@tukifact.net.pe"
BACKOFFICE_PASSWORD = "SuperAdmin2026!"
TIMEOUT = 30


def test_backoffice_employees_list():
    # Authenticate to get BACKOFFICE_TOKEN
    auth_url = f"{BASE_URL}{BACKOFFICE_AUTH_ENDPOINT}"
    auth_payload = {
        "email": BACKOFFICE_EMAIL,
        "password": BACKOFFICE_PASSWORD
    }

    try:
        auth_resp = requests.post(auth_url, json=auth_payload, timeout=TIMEOUT)
        auth_resp.raise_for_status()
    except requests.RequestException as e:
        assert False, f"Backoffice auth request failed: {e}"

    auth_data = auth_resp.json()
    token = auth_data.get("accessToken")
    assert token and token.startswith("eyJ"), "Invalid or missing accessToken in auth response"

    headers = {
        "Authorization": f"Bearer {token}"
    }

    # Request employees list
    employees_url = f"{BASE_URL}{EMPLOYEES_ENDPOINT}"
    try:
        emp_resp = requests.get(employees_url, headers=headers, timeout=TIMEOUT)
        emp_resp.raise_for_status()
    except requests.RequestException as e:
        assert False, f"GET employees request failed: {e}"

    try:
        employees = emp_resp.json()
    except ValueError:
        assert False, "Response is not valid JSON"

    assert isinstance(employees, list), "Employees response is not a JSON array"
    assert len(employees) >= 1, "Employees list is empty"

    # Check each employee has required fields with proper types
    required_fields = ['id', 'email', 'fullName', 'role', 'isActive']
    for emp in employees:
        assert isinstance(emp, dict), "Employee entry is not an object"
        for field in required_fields:
            assert field in emp, f"Employee missing field '{field}'"
        # Validate types
        assert isinstance(emp['id'], str) and emp['id'], "'id' must be a non-empty string"
        assert isinstance(emp['email'], str) and emp['email'], "'email' must be a non-empty string"
        assert isinstance(emp['fullName'], str) and emp['fullName'], "'fullName' must be a non-empty string"
        assert isinstance(emp['role'], str) and emp['role'], "'role' must be a non-empty string"
        assert isinstance(emp['isActive'], bool), "'isActive' must be a boolean"


test_backoffice_employees_list()