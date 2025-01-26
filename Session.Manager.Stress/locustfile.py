import random
import time
import uuid

import locust
from locust import HttpUser, between, task


class SessionUser(HttpUser):
    """
    User that simulates creating and reading sessions via a binary API,
    targeting multiple hosts at random.
    """
    host = "http://127.0.0.1:5001"
    wait_time = between(1, 2)

    def generate_random_data(self, min_size=1024, max_size=1024**2 * 10):
        """Generates a random byte array of a random size."""
        size = random.randint(min_size, max_size)
        return bytearray(random.getrandbits(8) for _ in range(size))

    @task
    def create_and_verify_session(self):
        """
        Creates a new session with random data, then reads it back and verifies
        the content.
        """
        session_id = str(uuid.uuid4())
        random_data = self.generate_random_data()

        # Create the session (PUT)
        with self.client.put(
            f"/session/{session_id}",
            data=random_data,
            headers={"Content-Type": "application/octet-stream"},
            catch_response=True,
            name="/session/{sessionId} (PUT)"
        ) as put_response:
            if put_response.status_code == 200 or put_response.status_code == 201:
                put_response.success()
            else:
                put_response.failure(
                    f"PUT failed with status code {put_response.status_code}")
                return
        # Wait a bit before reading the session back
        time.sleep(random.uniform(0.5, 2.0))
        # Read the session back (GET)
        with self.client.get(
            f"/session/{session_id}",
            headers={"Accept": "application/octet-stream"},
            catch_response=True,
            name="/session/{sessionId} (GET)"
        ) as get_response:
            if get_response.status_code == 200:
                if get_response.content == random_data:
                    get_response.success()
                else:
                    get_response.failure(
                        "GET response data does not match PUT data")
            else:
                get_response.failure(
                    f"GET failed with status code {get_response.status_code}")
