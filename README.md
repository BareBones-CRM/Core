# BareBones CRM 

BareBones CRM is a simplified CRM system designed for Microsoft's Power Platform - it's aim is to provide users with a quick and easy way to manage Sales and issues while only requiring a Per App license. The solution itself consists of 4 solutions which can be used individually (if you want just sales or customer service inside your own Application) or as a whole (if you just want to install it directly).

- BareBones Sales Core
- BareBones Customer Service Core
- BareBones CRM
- A Customised Dynamics 365 Outlook App (requires separate deployment once Dynamics 365 for Outlook is installed from https://appsource.microsoft.com/en-us/product/dynamics-365/mscrm.fa50aa98-e8bb-4757-83ce-6d607959b985?tab=overview )

# Overview


# BareBones Sales

Barebones Sales provides a means of creating leads, qualifying those leads into contacts / accounts / opportunities and then managing and tracking those opportunities. It is designed for companies where a straightforward opportunity tracking solution is required and where Quotes or Products are not required.

For GDPR reasons we've treated Leads as a separate entity rather than abusing contacts to ensure companies can differentiate between contacts (actual customers so required for business reasons) and leads (potential customers whose details should be deleted after a period of time).

# BareBones Customer Service

Barebones Customer Service is a simple solution designed to record and manage customer service inquiries. It allows cases to be easily created and automatically assigned to other users and teams based on case type with a simplified deadline management system included based on priority (which again can be automatically set based on case type).

# BareBones CRM

The BareBones CRM solution is a sample Application that provides users with the Functionality contained within the BareBones Sales and Customer Service Solutions to allow users to use the software. This solution is separate to the other solutions to allow you to use your own Application (with appropriate branding) instead of the BareBones Application.
