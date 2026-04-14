import requests

def test_plans_list_public():
    base_url = "http://localhost:80"
    url = f"{base_url}/v1/plans"
    timeout = 30

    try:
        response = requests.get(url, timeout=timeout)
        assert response.status_code == 200, f"Expected status 200 OK but got {response.status_code}"
        plans = response.json()
        assert isinstance(plans, list), "Response is not a JSON array"
        assert len(plans) >= 6, f"Expected at least 6 plans but got {len(plans)}"

        for plan in plans:
            assert 'name' in plan and isinstance(plan['name'], str) and plan['name'], "Plan missing 'name' or invalid"
            assert 'priceMonthly' in plan and (isinstance(plan['priceMonthly'], (int, float)) or plan['priceMonthly'] is None), \
                "Plan missing 'priceMonthly' or invalid type"
            assert 'maxDocumentsPerMonth' in plan and isinstance(plan['maxDocumentsPerMonth'], int), \
                "Plan missing 'maxDocumentsPerMonth' or invalid type"
            assert 'features' in plan and isinstance(plan['features'], list), "Plan missing 'features' or invalid type"
    except requests.RequestException as e:
        assert False, f"Request failed: {str(e)}"
    except ValueError as e:
        assert False, f"Invalid JSON response: {str(e)}"

test_plans_list_public()