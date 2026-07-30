namespace ShramSetu.Core.Enums;

public enum VerificationStatus { Pending, UnderReview, Verified, Rejected }
public enum DocumentType { Aadhaar, PAN, VoterID, WorkCertificate, Photo }
public enum EmployerType { Individual, Company, Contractor }
public enum SourcingStatus { Open, InProgress, Fulfilled, Cancelled }
public enum BookingType { DirectContact, ViaAdmin }
public enum BookingStatus { Requested, Confirmed, InProgress, Completed, Cancelled }
public enum NotificationChannel { SMS, WhatsApp, Email, Push }
public enum JobPostStatus { Open, Closed, Cancelled }
public enum JobApplicationStatus { Applied, Shortlisted, Accepted, Rejected, Withdrawn }

// Phase 2
public enum AvailabilitySlotType { Available, Unavailable, Booked }
public enum SubscriptionTier { Free, Basic, Pro, Enterprise }
public enum SubscriptionStatus { Active, Expired, Cancelled }
public enum ChatMessageStatus { Sent, Delivered, Read }

// Phase 3
public enum AttendanceStatus { Present, Absent, HalfDay, Holiday }
public enum PayrollStatus { Draft, Approved, Paid }
public enum BackgroundCheckStatus { Pending, InProgress, Passed, Failed }
public enum BackgroundCheckType { Criminal, Address, Employment, Reference }

// Sprint 1  Trust & Safety
public enum DisputeStatus { Open, UnderReview, Resolved, Dismissed }
public enum DisputeType { NonPayment, Mistreatment, Fraud, QualityIssue, Other }
public enum ReportType { FakeProfile, Scam, Harassment, InappropriateContent, Other }
public enum ReportStatus { Pending, Reviewed, ActionTaken, Dismissed }
public enum BadgeTier { PhoneVerified, AadhaarVerified, BackgroundCleared, InsuranceActive }

// Sprint 2  Worker Welfare
public enum LoanStatus { Active, Repaid, Defaulted }
public enum WalletTransactionType { Credit, Debit, Withdrawal, Refund }

// Sprint 3  Growth
public enum ReferralStatus { Pending, Verified, Rewarded }

// Sprint 4  Operations
public enum AuditAction { Created, Updated, Deleted, StatusChanged, LoginSuccess, LoginFailed }
public enum ContractStatus { Draft, Signed, Expired, Cancelled }

// Sprint 5  Technical
public enum SoftDeleteStatus { Active, Deleted }

// Sprint 6  Mobile
public enum AppPlatform { Android, iOS, Web }

// Sprint 7  Admin Power
public enum BanStatus { Active, Banned, Suspended }
public enum AnnouncementTarget { All, Workers, Employers, Admins }
public enum SlaStatus { OnTime, AtRisk, Breached }
public enum PlatformFeeType { BookingCommission, SubscriptionFee, SourcingFee }
public enum PlatformFeeStatus { Pending, Collected, Waived }

// Sprint 8  Worker Experience
public enum OnboardingStep { Photo, Skills, Location, Documents, Bank }
public enum SkillAssessmentStatus { NotAttempted, InProgress, Passed, Failed }
public enum EmergencyRelation { Father, Mother, Spouse, Sibling, Friend, Other }

// Sprint 9  Employer Experience
public enum FeatureFlagStatus { Enabled, Disabled }

// Sprint 10  Compliance & Legal
public enum ComplianceCheckType { MinimumWage, ESIC, PF, ContractValidity }
public enum ComplianceStatus { Compliant, Warning, NonCompliant }
public enum GstInvoiceStatus { Draft, Issued, Paid, Cancelled }

// Sprint 11  Session & Security
public enum SessionStatus { Active, LoggedOut, Expired, Revoked }



