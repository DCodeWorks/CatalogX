# CatalogX 🛒

**CatalogX** is a high-performance, scalable product catalog system built for modern e-commerce platforms. It’s designed to handle large-scale product data, enable lightning-fast search and filtering, and provide high availability using distributed system patterns.

> 📅 Built between **April 10, 2025** – **May 10, 2025**

---

## 🔍 Core Objective

To build a resilient, distributed catalog with a focus on:
- ⚡ **High-throughput read performance**
- 📈 **Horizontal scalability**
- 💡 **Smart caching and indexing strategies**
- 🔄 **Replication, partitioning, and reliability**

---

## ✨ Key Features

- **🔎 Product Search & Filtering**  
  Users can search by keyword and filter by attributes like category, price, and availability.

- **🚀 Optimized Read Path**  
  Strategic use of **indexes**, **caching**, and **partitioning** ensures sub-millisecond access during peak traffic.

- **📦 Distributed Data Storage**  
  Supports large datasets using **sharding** and **replication** for fault tolerance and performance.

---

## 🧠 Technical Highlights

### 📚 Architecture Principles

- **CQRS (Command Query Responsibility Segregation)**  
  Reads and writes are separated for scalability and maintainability.

- **Clean Architecture**  
  The project follows a layered approach with clear separation of concerns.

- **Polyglot Persistence**  
  Includes evaluation of SQL vs NoSQL trade-offs for specific operations.

---

### 🛠️ Infrastructure & Tooling

| Area             | Tech / Concept                       |
|------------------|--------------------------------------|
| Backend          | .NET 9                               |
| Data Storage     | SQL Server / MongoDB (evaluated)     |
| Caching Layer    | Redis                                |
| Caching Strategy | TTL + Invalidation                   |
| Search           | Indexes + Keyword matching           |
| Partitioning     | Horizontal sharding (per category)   |
| Deployment       | Docker + Docker Compose              |
| Observability    | Integrated logging + tracing hooks   |

---

## 🧰 Example Features

- `GET /products?category=electronics&sort=price_asc`
- `GET /products/search?q=wireless+mouse`
- Redis-backed caching of product queries with TTL
- Support for pagination, price filters, availability checks

---

## 💡 Scalability Strategy

| Technique              | Purpose                              |
|------------------------|--------------------------------------|
| **Sharding**           | Distributes products across nodes    |
| **Replication**        | Ensures data redundancy              |
| **Redis Caching**      | Offloads repeated read traffic       |
| **Secondary Indexes**  | Accelerates search queries           |
| **CDN (optional)**     | For static catalog assets            |

---

## 🤔 SQL vs NoSQL Decision

- ✅ Used **SQL** where **consistency** and **relational integrity** are critical (e.g., admin product management).
- ✅ Explored **NoSQL** (e.g., MongoDB) for **product read models**, allowing flexible schema and better scale-out.

---

## 🛠 Getting Started

```bash
# Clone the repository
git clone https://github.com/DCodeWorks/CatalogX.git
cd CatalogX

# Start services (Redis, DB, etc.)
docker-compose up -d

# Run the application
dotnet run --project CatalogX.API
