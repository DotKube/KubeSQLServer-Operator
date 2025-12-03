---
sidebar_position: 1
---

# Introduction

Welcome to the **KubeSQLServer Operator** documentation!

KubeSQLServer Operator is a completely free and open-source (MIT licensed) Kubernetes operator designed to help you run and manage Microsoft SQL Server seamlessly in Kubernetes.

## What is KubeSQLServer Operator?

KubeSQLServer Operator automates the deployment, configuration, and management of SQL Server instances in Kubernetes clusters. Whether you need to run SQL Server inside your cluster or manage external SQL Server instances, this operator has you covered.

## Key Features

- 🚀 **In-Cluster SQL Server** - Deploy SQL Server as StatefulSets with persistent storage
- 🔗 **External SQL Server Support** - Manage external instances (Azure SQL, AWS RDS, on-premises)
- 🗄️ **Database Management** - Declaratively manage databases, schemas, logins, and users
- 📦 **Helm Distribution** - Easy installation via Helm charts
- 🔓 **Completely Free** - MIT licensed, no restrictions

## Intended Use

**⚠️ This operator is designed for development, QA, and local testing environments only.**

It is **NOT intended for production use**. For production workloads, consider:

- Managed database services (Azure SQL, AWS RDS)
- Enterprise-grade database operators
- SQL Server Always On Availability Groups
- Professional database administration

## Why Use This Operator?

This project is an open-source alternative to commercial SQL Server operators for non-production environments. It provides:

- Declarative database management using Kubernetes CRDs
- Automated database provisioning and user management
- Support for both in-cluster and external SQL Server instances
- Easy setup for development and testing workflows
- Clean resource cleanup with finalizers
