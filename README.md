# Airmaster Global E-Commerce Order Management Solution

This repository contains a full-stack solution for the Airmaster coding evaluation exercise.

## Contents

- `docs/design.md` — design document with architecture diagram, database schema, microservice boundaries, scaling strategy, and security considerations.
- `backend/` — ASP.NET Core Web API backend in C#.
- `frontend/` — Angular frontend skeleton with TypeScript, HTML, and CSS.

## Technologies

- Backend: C# / ASP.NET Core Web API
- Frontend: Angular / TypeScript / HTML / CSS
- Design: Markdown with Mermaid architecture diagram

## How to use

1. Open the `backend` folder in Visual Studio or VS Code and restore NuGet packages.
2. Open the `frontend` folder and run `npm install` to restore Angular packages.
3. Start the backend API and frontend app for local development.
   - In `frontend`, use `npm start` to launch Angular with a proxy to the local backend.
4. Use the SPA to browse products, create an order, process payment, and simulate shipping.

## New Features

- Order creation and validation with dynamic product pricing.
- Simulated payment processing with retry-friendly failure handling.
- Shipping simulation endpoint and tracking number generation.
- Angular checkout flow with form fields, status messages, and polling.

> Note: This repository includes a working evaluation implementation, plus a detailed system design document in `docs/design.md`.
