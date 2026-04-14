import requests
import uuid

BASE_URL = "http://localhost:80"
BACKOFFICE_AUTH_URL = f"{BASE_URL}/v1/backoffice/auth/login"
EMPLOYEES_URL = f"{BASE_URL}/v1/backoffice/employees"
BACKOFFICE_EMAIL = "superadmin@tukifact.net.pe"
BACKOFFICE_PASSWORD = "SuperAdmin2026!"
TIMEOUT = 30


def test_backoffice_employees_crud():
    # Authenticate to get BACKOFFICE_TOKEN
    auth_payload = {
        "email": BACKOFFICE_EMAIL,
        "password": BACKOFFICE_PASSWORD
    }
    try:
        auth_response = requests.post(BACKOFFICE_AUTH_URL, json=auth_payload, timeout=TIMEOUT)
        assert auth_response.status_code == 200, f"Backoffice login failed: {auth_response.status_code} {auth_response.text}"
        auth_json = auth_response.json()
        token = auth_json.get("accessToken")
        assert token and token.startswith("eyJ"), "Invalid accessToken in backoffice login response"
        user = auth_json.get("user")
        assert user and user.get("role") == "superadmin", "User role is not superadmin"
    except Exception as e:
        raise AssertionError(f"Failed to authenticate backoffice user: {e}")

    headers = {"Authorization": f"Bearer {token}"}

    employee_id = None

    unique_email = f"support_prd_{uuid.uuid4()}@tukifact.net.pe"

    # Step 1: POST /v1/backoffice/employees to create employee
    employee_create_payload = {
        "email": unique_email,
        "fullName": "Soporte PRD",
        "password": "Support2026!",
        "role": "support"
    }
    try:
        post_resp = requests.post(EMPLOYEES_URL, json=employee_create_payload, headers=headers, timeout=TIMEOUT)
        assert post_resp.status_code == 201, f"Employee creation failed: {post_resp.status_code} {post_resp.text}"
        employee = post_resp.json()
        employee_id = employee.get("id")
        assert employee_id, "Employee ID not returned on creation"
    except Exception as e:
        raise AssertionError(f"Failed at employee creation: {e}")

    try:
        # Step 2: PUT to update employee fullName
        update_name_payload = {"fullName": "Soporte PRD Updated"}
        put_name_url = f"{EMPLOYEES_URL}/{employee_id}"
        put_name_resp = requests.put(put_name_url, json=update_name_payload, headers=headers, timeout=TIMEOUT)
        assert put_name_resp.status_code == 200, f"Updating employee fullName failed: {put_name_resp.status_code} {put_name_resp.text}"

        # Step 3: PUT to update employee role
        update_role_payload = {"role": "ops"}
        put_role_url = f"{EMPLOYEES_URL}/{employee_id}/role"
        put_role_resp = requests.put(put_role_url, json=update_role_payload, headers=headers, timeout=TIMEOUT)
        assert put_role_resp.status_code == 200, f"Updating employee role failed: {put_role_resp.status_code} {put_role_resp.text}"

    finally:
        # Step 4: DELETE employee
        try:
            delete_url = f"{EMPLOYEES_URL}/{employee_id}"
            delete_resp = requests.delete(delete_url, headers=headers, timeout=TIMEOUT)
            assert delete_resp.status_code == 200, f"Employee deletion failed: {delete_resp.status_code} {delete_resp.text}"
            delete_json = delete_resp.json()
            message = delete_json.get("message","").lower()
            assert "desactivado" in message, "Delete response message does not contain 'desactivado'"
        except Exception as e_del:
            raise AssertionError(f"Failed to delete employee in cleanup: {e_del}")


test_backoffice_employees_crud()
