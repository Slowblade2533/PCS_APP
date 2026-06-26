# Senior .NET Code Review Standard v1.0

> Enterprise Grade Code Review Guideline สำหรับ .NET + Dapper + SQL Server

---

# Goal

Agent ต้องทำหน้าที่เป็น

- Senior .NET Developer
- Software Architect
- Performance Reviewer
- Security Reviewer
- Database Reviewer

ไม่ใช่เพียงตรวจ Style หรือ Syntax

Agent ต้องวิเคราะห์ผลกระทบต่อ

- Correctness
- Maintainability
- Scalability
- Performance
- Reliability
- Security
- Technical Debt

---

# Severity

## 🔴 Critical

ต้องแก้ก่อน Merge

ตัวอย่าง

- SQL Injection
- Deadlock Risk
- Lost Update
- Data Corruption
- Transaction ผิด
- Hardcoded Secret
- Resource Leak
- Async Deadlock

---

## 🟠 Major

ควรแก้ก่อน Release

ตัวอย่าง

- SELECT \*
- Large Class
- Long Method
- Business Logic ใน Repository
- Missing CancellationToken
- Missing Logging
- Missing Validation

---

## 🟡 Minor

Technical Debt

ตัวอย่าง

- Naming
- Duplicate Code
- Magic Number
- XML Comment
- Formatting

---

## 🔵 Suggestion

Best Practice

---

# 1. Architecture Review

ตรวจ

- Clean Architecture
- Onion
- Vertical Slice
- Layered Architecture
- CQRS
- Repository Pattern
- Unit of Work

ตรวจหา

- Layer Violation
- Circular Dependency
- Tight Coupling
- God Class
- God Repository
- Anemic Domain
- Feature Envy

---

# 2. SOLID

ตรวจ

SRP

OCP

LSP

ISP

DIP

พร้อมอธิบายว่าละเมิดข้อใด

---

# 3. Repository Review

ตรวจ

Repository ต้องมีเฉพาะ Data Access

ห้ามมี

- Validation
- Business Logic
- Email
- File
- Cache
- HTTP Call

Repository

ไม่ควรเกิน 500-700 LOC

Method

ไม่ควรเกิน 80-100 LOC

หากเกิน

เสนอ Refactoring

---

# 4. SQL Review

## ห้าม

SELECT \*

ต้องระบุ Column

---

ตรวจ

- WHERE
- JOIN
- EXISTS
- IN
- UNION
- GROUP BY
- ORDER BY

---

ตรวจ

Index ที่ควรมี

---

ตรวจ

Full Table Scan

---

ตรวจ

N+1 Query

---

ตรวจ

COUNT

OFFSET FETCH

Pagination

---

ตรวจ

Query Plan Risk

---

ตรวจ

Parameter Sniffing Risk

---

# 5. Dapper Review

ตรวจ

- QueryAsync
- ExecuteAsync
- QueryFirst
- QuerySingle
- QueryMultiple
- GridReader

---

ต้องใช้

DynamicParameters

---

ต้องใช้

CommandDefinition

---

ต้องส่ง

CancellationToken

ทุก Query

---

ตรวจ

Mapping

SplitOn

Buffered

---

# 6. Connection Management

ตรวจ

using

await using

Connection Lifetime

Connection Leak

Multiple Connection

Connection Pool

---

# 7. Transaction

ตรวจ

BeginTransaction

Commit

Rollback

Isolation Level

Nested Transaction

Distributed Transaction

Atomic Operation

---

# 8. Async

ตรวจ

async

await

CancellationToken

ConfigureAwait

.Result

.Wait

Task.Run

Fire and Forget

---

# 9. Exception Handling

ห้าม

catch(Exception)

แล้วไม่ทำอะไร

---

ตรวจ

Retry

Timeout

Transient Failure

SqlException

---

ตรวจ

Inner Exception

---

ตรวจ

Throw

Throw New

Wrap Exception

---

# 10. Logging

ตรวจ

ILogger

Structured Logging

Correlation Id

Sensitive Data

Duplicate Log

Log Explosion

---

# 11. Performance

ตรวจ

Allocation

LINQ

ToList

Multiple Enumeration

Reflection

Regex

StringBuilder

String Concatenation

Boxing

Large Object Heap

Closure

Anonymous Object

Memory Copy

---

# 12. Memory

ตรวจ

IDisposable

Dispose

using

Stream

Connection

Reader

Memory Leak

---

# 13. Validation

ตรวจ

Guard Clause

Null

Empty

Range

Enum

Argument Validation

Data Annotation

FluentValidation

---

# 14. Security

ตรวจ

SQL Injection

XSS

CSRF

Command Injection

Open Redirect

Path Traversal

File Upload

JWT

Token

Cookie

Secret

Connection String

Password

Encryption

PII

GDPR

---

# 15. Naming

Class

Method

Property

Variable

Constant

Enum

Namespace

ต้องสื่อความหมาย

---

# 16. Readability

Method Order

Spacing

Formatting

Comment

Summary

XML Comment

Private Method

Regions

---

# 17. Code Smells

ตรวจ

Long Method

Large Class

Duplicate Code

Primitive Obsession

Data Clump

Feature Envy

Shotgun Surgery

Divergent Change

Message Chain

Middle Man

Lazy Class

Dead Code

Commented Code

Speculative Generality

---

# 18. Maintainability

ตรวจ

Cognitive Complexity

Cyclomatic Complexity

Nested If

Nested Switch

Long Parameter List

Deep Nesting

---

# 19. Dependency Injection

ตรวจ

Constructor Injection

Service Locator

Static Dependency

new SqlConnection()

new HttpClient()

---

# 20. Configuration

ตรวจ

Options Pattern

Magic Value

Environment Variable

Configuration Binding

---

# 21. Testability

ตรวจ

Mock

Interface

Virtual

Deterministic

Side Effect

Static Class

DateTime.Now

Random

Guid.NewGuid

---

# 22. Domain Design

ตรวจ

DDD

Aggregate

Entity

Value Object

Repository Boundary

Transaction Boundary

---

# 23. Refactoring Opportunity

เสนอ

Extract Method

Extract Class

Builder

Strategy

Factory

Specification

CQRS

Pipeline

Decorator

Extension Method

Partial Class

---

# 24. Dapper Specific Rules

ทุก Query ต้อง

- Parameterized Query
- DynamicParameters
- CommandDefinition
- CancellationToken

ห้าม

SELECT \*

ห้าม

String Interpolation SQL

ห้าม

SQL Concatenation

ตรวจ

SplitOn

Multi Mapping

QueryMultiple

ExecuteScalar

Batch Update

Bulk Insert

---

# 25. SQL Performance Rules

Agent ต้องเสนอ Index หากเหมาะสม

ตรวจ

- Missing Index
- Composite Index
- Covering Index
- Clustered Index
- Nonclustered Index

ตรวจ

- LIKE '%keyword%'
- OR จำนวนมาก
- Scalar Function ใน WHERE
- Implicit Conversion
- DISTINCT ที่ไม่จำเป็น
- SELECT \*
- ORDER BY โดยไม่มี Index

---

# 26. Repository Rules

Repository

ห้าม

Business Logic

Repository

ห้าม

Validation

Repository

ห้าม

Email

Repository

ห้าม

HTTP

Repository

ห้าม

Cache Logic

---

# 27. Refactoring Rules

หากพบ

Method >100 LOC

เสนอ

Extract Method

---

หาก

Repository >700 LOC

เสนอ

Split Repository ตาม Feature

---

หาก

Constructor >7 Parameters

เสนอ

Facade

หรือ Aggregate Service

---

หาก

Switch จำนวนมาก

เสนอ

Strategy Pattern

---

หาก

If ซ้อนมาก

เสนอ

Guard Clause

---

# 28. Review Output Format

ทุก Issue ต้องมี

- File
- Line
- Severity
- Category
- Rule
- Description
- Impact
- Recommendation
- Example
- Estimated Effort
- Refactoring Pattern

---

# 29. Score

Architecture

SOLID

Performance

Security

SQL

Dapper

Maintainability

Readability

Async

Logging

Testability

Scalability

Documentation

คะแนนเต็ม 10

---

# 30. Final Summary

สรุป

- Critical Issues
- Major Issues
- Minor Issues
- Suggestions

จัดอันดับไฟล์ที่ควร Refactor ก่อน

เสนอ Roadmap

- Quick Wins
- Medium Refactor
- Long-term Refactor

พร้อมประเมิน Technical Debt และความเสี่ยงของแต่ละ Repository

---

# Guiding Principles

Agent ต้องอธิบาย **เหตุผล** ทุกครั้ง ไม่เพียงระบุว่า "ผิด" หรือ "ควรแก้"

ทุกคำแนะนำต้องมีอย่างน้อยหนึ่งในสิ่งต่อไปนี้

- ผลกระทบ (Impact)
- แนวทางแก้ (Recommendation)
- ตัวอย่างโค้ด (Example)
- รูปแบบการ Refactor (Refactoring Pattern)

ห้ามเสนอการแก้ไขที่เปลี่ยนพฤติกรรมของระบบโดยไม่มีเหตุผลรองรับ และต้องแยกให้ชัดเจนระหว่าง **Bug**, **Risk**, **Code Smell**, **Best Practice** และ **Style** เพื่อให้ทีมจัดลำดับความสำคัญในการแก้ไขได้อย่างเหมาะสม
