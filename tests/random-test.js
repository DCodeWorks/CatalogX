import http from 'k6/http';
import { check, sleep } from 'k6';

const API_BASE = 'https://localhost:7244/api/products';

export const options = {
    insecureSkipTLSVerify: true,
    scenarios: {
        paginationTest: {
            executor: 'constant-vus',
            exec: 'pagination',
            vus: 10,
            duration: '1m',
        },
        searchTest: {
            executor: 'constant-vus',
            exec: 'search',
            vus: 5,
            duration: '1m',
            startTime: '0s',
        },
        categoryTest: {
            executor: 'constant-vus',
            exec: 'category',
            vus: 5,
            duration: '1m',
            startTime: '0s',
        },
        creationTest: {
            executor: 'constant-vus',
            exec: 'creation',
            vus: 2,
            duration: '10s',
            startTime: '50s',
        },
        updateTest: {
            executor: 'constant-vus',
            exec: 'updateItem',
            vus: 2,
            duration: '10s',
            startTime: '50s',
        },
        deleteTest: {
            executor: 'constant-vus',
            exec: 'deleteItem',
            vus: 2,
            duration: '10s',
            startTime: '50s',
        },
        cacheTest: {
            executor: 'constant-vus',
            exec: 'cacheTest',
            vus: 5,
            duration: '50s',
            startTime: '0s',
        }
    },
    thresholds: {
        'http_req_duration{scenario:paginationTest}': ['p(95)<300'],
        'http_req_duration{scenario:searchTest}': ['p(95)<500'],
        'http_req_duration{scenario:categoryTest}': ['p(95)<300'],
        'http_req_duration{scenario:creationTest}': ['p(95)<500'],
        'http_req_duration{scenario:updateTest}': ['p(95)<500'],
        'http_req_duration{scenario:deleteTest}': ['p(95)<500'],
        'http_req_duration{scenario:cacheTest}': ['p(95)<50'],
        'http_req_failed': ['rate<0.01'],
    },
};

export function pagination() {
    const page = Math.floor(Math.random() * 500) + 1;
    const res = http.get(
        `${API_BASE}?PageNumber=${page}&PageSize=20`,
        { tags: { scenario: 'paginationTest' } }
    );
    
    check(res, {
        'pagination 200': (r) => r.status === 200,
        'has items': (r) => {
            if (r.status !== 200) return false;
            const body = r.json();
            return Array.isArray(body.data) && body.data.length > 0;
        },
    });
    sleep(0.1);
}

export function search() {
    const id = Math.floor(Math.random() * 1_000_000) + 1;
    const term = encodeURIComponent(`Description for product ${id}`);
    const res = http.get(
        `${API_BASE}?Search=${term}`,
        { tags: { scenario: 'searchTest' } }
    );
    check(res, {
        'search 200': (r) => r.status === 200,
        'found items': (r) => {
            if (r.status !== 200) return false;
            const body = r.json();
            return Array.isArray(body.data) && body.data.length > 0;
        },
    });
    sleep(0.1);
}

export function category() {
    const cat = `Category ${Math.floor(Math.random() * 5) + 1}`;
    const res = http.get(
        `${API_BASE}?Category=${encodeURIComponent(cat)}`,
        { tags: { scenario: 'categoryTest' } }
    );
    check(res, {
        'category status 200': (r) => r.status === 200,
        'some items returned': (r) => r.json().data?.length > 0,
    });
    sleep(0.1);
}

export function creation() {
    const id = Math.floor(Math.random() * 6_000_000) + 1;
    const payload = JSON.stringify({
        name: `Product${id}`,
        description: `Description for product ${id}`,
        price: parseFloat((Math.random() * 100).toFixed(2)),
        category: `Category ${Math.floor(Math.random() * 5) + 1}`,
    });
    const res = http.post(
        API_BASE,
        payload,
        {
            tags: { scenario: 'creationTest' },
            headers: { 'Content-Type': 'application/json' },
        }
    );
    check(res, {
        'creation status 201': (r) => r.status === 201 || r.status === 200,
        'created id present': (r) => r.json().id !== undefined,
    });
    sleep(0.1);
}

export function updateItem() {
    // pick a random existing ID
    const id = Math.floor(Math.random() * 5_000_000) + 1;
    const payload = JSON.stringify({
        name: `UpdatedProduct${id}`,
        description: `Updated description for product ${id}`,
        price: parseFloat((Math.random() * 100).toFixed(2)),
        category: `Category ${Math.floor(Math.random() * 5) + 1}`,
    });
    const res = http.put(
        `${API_BASE}/${id}`,
        payload,
        {
            tags: { scenario: 'updateTest' },
            headers: { 'Content-Type': 'application/json' },
        }
    );
    check(res, { 'update status 200|204': (r) => r.status === 200 || r.status === 204 });
    sleep(0.1);
}

export function deleteItem() {
    const id = Math.floor(Math.random() * 5_000_000) + 1;
    const res = http.del(
        `${API_BASE}/${id}`,
        null,
        { tags: { scenario: 'deleteTest' } }
    );
    check(res, { 'delete status 200|204': (r) => r.status === 200 || r.status === 204 });
    sleep(0.1);
}

export function cacheTest() {
    const url = `${API_BASE}`
        + '?PageNumber=1'
        + '&PageSize=20'
        + '&Category=Category%201';

    const res1 = http.get(url, { tags: { scenario: 'cacheTest' } });

    check(res1, {
        'cacheTest first fetch status 200': (r) => r.status === 200
    });

    sleep(0.2);

    const res2 = http.get(url, { tags: { scenario: 'cacheTest' } });
    check(res2, {
        'cacheTest second fetch status 200': (r) => r.status === 200,
        'cacheTest fast response <50ms': (r) => r.timings.duration < 50
    });

    sleep(0.2);
}
