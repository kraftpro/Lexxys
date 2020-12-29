// FoundationSource Financial System.
// file: Cnst.cs
//
// Copyright (c) 2001-2013 Foundation Source Philanthropic Services Inc. All rights reserved.
//
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Lexxys;

namespace FoundationSource.Admin.Common
{
	public static class Cnst
	{
		public const int ActionsLogQuery = 1;
		public const int ActionsLogDeleteFoundation = 2;
		public const int FoundationSource = 1;
		public const string Us = "US";
		public const int ServiceProviderAlternativeAsset = 744;
	}

	[Flags]
	public enum DocumentViewModes
	{
		View = 1,
		Download = 2,
		ConvertToPdf = 4,
		//ReplaceImagesToScript = 8,
		//ReplaceImagesToFilePath = 12
	}

	public enum DocumentViewType
	{
		Document,
		Image,
		Report,
	}

	public enum ExpenseAdditionalFiling
	{
		NotSet = -1,
		None = 0,
		Form1099 = 1,
		Form1042 = 2,
		W2Adjustments = 3
	}

	public enum GrantTypeCode
	{
		Generic = 1,
		Approval = 2,
		Request = 3,
		Recommendation = 4,
	}

	public enum GrantRecipientType
	{
		Charity = 0,
		Household = 1,
		Request = 2,
	}

	public enum CertificateSteps
	{
		Generated = 0,
		Cancelled = -1,
		Expired = -3,
	}

	[Flags]
	public enum GrantTypes
	{
		OneTime = 0,
		MultiPart = 1,
		Recurring = 2,
		GrantTemplate = 4,
		Historical = 8,
		OutsideSystem = 16,
		WithCharityRequest = 32,
		Recomendation = 64,
		CertificateRecipients = 128,
	}

	public enum HistoricalGrantType
	{
		Other = 0,
		Historical = 1,
	}

	[Flags]
	public enum NomineeAppointment
	{
		President = 1,
		VcPresident = 2,
		Director = 4,
		Secretary = 8,
		Recipient = 16,
		RelatedPerson = 32,
		GrantCommitteeMember = 64,
	}

	public enum NomineeStep
	{
		Generated = 0,
		Active = 99,
		Canceled = -1,
		Fired = -2,
	}

	[Flags]
	public enum NomineeStepChangeType
	{
		Position = 1,
		Step = 2,
		All = 3,
	}

	public enum RegistrationSteps
	{
		Back = -1,
		First = 0,
		Complete = 99,
	}

	public enum TimePeriodType
	{
		Day = 1,
		Week = 7,
		Decade = 8,
		Month = 11,
		Quarter = 13,
		HalfYear = 15,
		Year = 19,
	}

	public static class EftpsTaxType
	{
		public const int PfTaxFederalDeposit = 99036;
		public const int PfTaxPaymentDueReturn = 99037;
		public const int TTaxTaxFederalDeposit = 99046;
		public const int TTaxAmountWithReturn = 99047;
		public const int TTaxAmountWithExtension = 99042;
	}

	public enum PendingTransactStep
	{
		Removed = -3,
		Canceled = -2,
		OnHold = -1,
		Generated = 0,
		BridgerMatch = 3,
		BridgerManualResolved = 4,
		ReviewExpirationDate = 5,
		ReviewControlGrant = 10,
		CrossAffiliation = 15,
		SelfDealing = 20,
		ReadyForApprove = 25,
		FinalQFee = 5,
		AwaitingConfirmation = 27,
		PendingCash = 30,
		TransactionGenerated = 31,
		ReadyForTransfer = 35,
		TransferProcessing = 36,
		TransferSent = 40,
		AwaitingMatching = 70,
		Matched = 80,
		CheckInstructions = 83,
		PendingCheck = 85,
		Done = 99,

		WaitingRequest = -4,
		WaitingApproval = -5,
		Recommendation = -6,
		CanceledRecommendation = -7,
	}

	public enum GrantState
	{
		Normal = 0,
		Hold = 1,
		PendingCancelation = 2,
		PendingApproval = 3,
		Redo = 4,
	}

	public enum FinAdvisorRole
	{
		PrimaryAdvisor = 101,
		SecondaryAdvisor = 102,
		TaxAdvisor = 103,
		LegalAdvisor = 104,
		WealthAdvisor = 105,
	}

	public enum ExpenseSubCategory
	{
		PremiumPayment = 17,
		AgencyIn = 67,
		EstimatedTaxPayment = 69,
		TaxExtentionOrReturn = 70,
	}

	public enum ExpenseStep
	{
		Matching = -15,
		Template = -10,
		Canceled = -2,
		AdminDraft = 0,
		Draft = 1,
		Questions = 4,
		Review = 5,
		PcaReview = 10,
		LegalReview = 15,
		NeedMoreInfo = 20,
		FinOpsReview = 25,
		RequestW9 = 26,
		UserApproval = 27,
		UserComments = 29,
		Closed = 99,
	}

	public enum ExpenseIssueType
	{
		MissingAttachments = 1,
		IamNotSure = 2,
		InvestmentCharitablePurpose = 3,
		DisqualifiedPersonReview = 4,
		DisqualifiedPersonCompliance = 5,
		InsuranceReview = 6,
		VendorReviewFinOps = 7,
		TravelExpenseReimburcement = 8,
		AssetPurchaseReview = 9,
		FundraisingEvent = 10,
		VendorReviewCS = 11
	}

	public enum ExpenseRequestW9Type
	{
		Email = 1,
		Offline = 2
	}

	public enum LocalAssetReviewIssueStep
	{
		NotDetected = 0,
		NotIssue = 99,
		Issue = 1,
		ManuallyOpened = -1,
		ManuallyClosed = -99
	}
	public enum AssetValuationType
	{
		Purchase = 1,
		Donation = 2,
		Regular = 3,
		Sell = 4,
		BeginningBalance = 5,
	}

	public enum AssetValuationFrequency
	{
		Quarterly = 1,
		SemiAnnual = 2,
		Annual = 3,
		Every5Years = 4
	}

	public enum AssetPriceSourceType
	{
		Automatic = 0,
		Manual = 1,
		FileUploaded = 2,
		Yahoo = 100,
	}

	public enum VoitingProposalType
	{
		StaffingAction = 1,
		Grant = 2,
		GrantApproval = 3,
	}

	public enum VotingType
	{
		Unanimous = 1,
		ByMajority = 2,
		InvertedUnanimous = 3,
	}

	public enum VotingStep
	{
		Closed = -3,
		Rejected = -1,
		Created = 0,
		Approved = 99,
	}

	public enum VotingPersonFilter
	{
		StaffingRights = 1,
		GrantCommitteMembership = 2,
		GrantApprovingPersons = 3,
	}

	public enum VotingResultStep
	{
		Rejected = -1,
		Undecided = 0,
		Approved = 99,
	}

	public enum ProposalStep
	{
		Suspended = -5,
		Deleted = -4,
		Expired = -3,
		Rejected = -1,
		Created = 0,
		Approved = 99,
	}

	public enum VoteActions
	{
		Reserved = 1,
		Appoint = 2,
		Reelect = 3,
		Remove = 4,
	}

	public enum ProposalAction
	{
		Approve = 1,
		Reject = 2,
		Expired = 3,
		ProcessNew = 4,
		Delete = 5,
		ChangeToOffline = 6,
	}

	public enum CharityReqStep
	{
		New = 0,
		UnderResearch = 1,
		AwaitingApproval = 2,
		AwaitingRejection = 3,
		Completed = 99,
		Rejected = -1,
		Canceled = -2,
	}

	public enum CharityReqStatus
	{
		Normal = 0,
		AfterApproval = 1,
		AfterRejection = 2,
		CharityLocated = 3,
	}

	public enum CharityRequestPending
	{
		Nr = 10,
		TaxLegal = 20,
		CharityInfo = 30,
		PcaFoundationInfo = 40,
		FiveOuOnePending = 50,
		Supporting = 60,
		Unresponsive = 70,
		Na = 1,
	}

	public enum CharityRequestType
	{
		Normal = 1,
		GrantHold = 2,
		ProfileChange = 3,
	}

	public enum CharityStatusCode
	{
		Auto = 0,
		Manual = 1,
		Inactive = 2,
		Unverified = 3,
		Other = 4,
	}

	public enum CharityStep
	{
		Remove = -2,
		Hold = -1,
		New = 0,
		Problem = 70,
		Active = 99,
	}

	public enum CharityHold
	{
		None = 0,
		Auto = 1,
		Manual = 2,
	}

	public enum FoundationAccountStatus
	{
		Normal = 0,
		Inactive = -1,
		Disabled = -2,
	}

	public enum DonorDeliveryMethod
	{
		None = 0,
		Print = 1,
		Email = 2,
	}

	public enum RecipientTypes
	{
		Global = 1,
		System = 2,
		EventRelated = 3,
		Custom = 4,
	}

	public enum EmailAddressGroups
	{
		To = 1,
		Cc = 2,
		Bcc = 3,
		From = 4,
	}

	public enum EventSource
	{
		Foundation = 1,
		TimeSchedule = 2,
		Fake = 3,
	}

	public enum EventType
	{
		System = 1,
		TimeSchedule = 2,
		Manual = 3,
	}

	public enum JobState
	{
		Created = 0,
		Ready = 2,
		Starting = 3,
		Started = 4,
		Done = 99,
		Retried = -1,
		Failed = -2,
	}

	public enum EventState
	{
		Fired = 0,
		Registered = 1,
		Ready = 2,
		Starting = 3,
		Started = 4,
		PartiallyCompleted = 80,
		Done = 99,
		Invalid = -2,
	}

	public enum JobClassCommand
	{
		ProcessEmail = 1,
		ExecuteCommand = 2,
		CreateExpense = 3,
		ExecuteApMethod = 4,
		SpamEmail = 5,
	}

	public enum JobSubType
	{
		Default = 0,
		SendEmail = 1,
		GenerateEmail = 2,
		GenerateTask = 3,
	}

	public enum MessageState
	{
		Created = 0,
		Sent = 99,
		Cancel = -1,
	}

	public enum DocumentTemplateFormat
	{
		Unknown = 0,
		LegaccyText = 1,
		WordRtf = 2,
		Html = 3,
		WordHtml = 4,
		ExcelXml = 5,
		Xsl = 6,
		Text = 7,
		WordXml = 8,
	}

	public enum DocumentOutputFormat
	{
		None = 0,
		Html = 1,
		Word = 2,
		Excel = 3,
		Pdf = 4,
		TextData = 5,
	}

	public enum DocumentCategory
	{
		DocCatCharterAndCorporate = 1,
		DocCatCorrespondence = 2,
		DocCatTaxDocuments = 3,
		DocCatFsContracts = 4,
		DocCatFast = 5,
		DocCatMiscellaneous = 6,
	}

	public enum StructuredCompanyType
	{
		StandardFoundation = 1,
		MfsFoundation = 2,
		CorporateFoundation = 3,
		ServiceProvider = 4,
		Bank = 5,
		FoundationSource = 6,
		MemberStandardFoundation = 7
	}

	public enum PredefinedSysPosition
	{
		President = 1,
		VicePresident = 2,
		Director = 4,
		CertificateRecipient = 16,
		RelatedPerson = 32,
		Secretary = 33,
		Treasurer = 34,
		FoundationRep = 37,
		RelationshipManager = 40,
		CsAdmin = 41,
		SvpFinOps = 42,
		FinOpsManager = 43,
		CheckProcessor = 44,
		RecSpecialist = 45,
		ImplManager = 46,
		ImplSpecialist = 47,
		SvpLegal = 48,
		VpLegal = 49,
		TaxSpecialist = 50,
		ParaLegal = 51,
		TaxAdmin = 52,
		BusinessAnalyst = 53,
		ViewOnly = 54,
		SalesRep = 55,
		BankOfficer = 60,
		Advisor = 61,
		ExternalAdministrator = 63,
		ContactPerson = 98,
		TaxAdvisor = 99,
		LegalAdvisor = 100,
		KeyAccOwner = 101,
		FinancialAnalyst = 1006,
		PhilDir = 1010,
		FoundationContact = 1017
	}

	public enum ContractDefinition
	{
		None = 0,
		Contractor = 1,
		Customer = 2,
		RelationshipManager = 3,
		ResponsibleDepartment = 4,
		StrategicPartner = 6,
		SalesRepresentative = 7,
		PhilanthropicDirector = 8,
		KeyAccountOwner = 9,
		FinancialAnalyst = 10,
		PrimaryAdvisor = 101,
		SecondaryAdvisor = 102,
		TaxAdvisor = 103,
		LegalAdvisor = 104,
		WealthAdvisor = 105
	}

	public enum ServiceDefinition
	{
		Proc = 22,
		ProcessingCheckWriting = 23,
		ProcessingTransfers = 28,
		TenZeroNewGrantMaking = 31,
		PremierCs = 33,
		TerminationWithTaxes = 43,
		TerminationNoTaxes = 44,
		TerminationAndDissolution = 45,
		Top50Priority = 54,
	}

	public enum QuestionnaireStep
	{
		Generated = 0,
		SaveDraft = 1,
		Submitted = 2,
		LegalReview = 3,
		Completed = 4,
	}

	public enum SectionResolved
	{
		No = 0,
		Yes = 1,
		Unknown = 2,
	}

	public enum CheckVoidMode
	{
		None = 0,
		Reissue = 1,
		Void = 2,
		Cancel = 3,
		VoidSplit = 4,
	}

	public enum PaymentType
	{
		Check = 1,
		Eftps = 2,
		WireTransfer = 3,
		Other = 4,
	}

	public enum PaymentStep
	{
		Cancelled = -1,
		Created = 0,
		PendingCheckModification = 10,
		WaitingForApproval = 15,
		BridgerMatch = 21,
		BridgerResolved = 22,
		NotReady = 24,
		Ready = 25,
		Sent = 30,
		Processed = 90,
		Cleared = 99,
	}

	public enum OriginalCheckSent
	{
		Charity = 1,
		Vendor = 2,
		Client = 3,
		CheckNeverMailed = 4,
	}

	public enum EnrollmentStep
	{
		Na = -1,
		NotEnrolled = 0,
		Pending = 1,
		Enrolled = 2,
		ReEnrolled = 3,
	}

	public enum FoundationNoteCategorySection
	{
		CustomerServiceNotes = 1,
		OperationsNotes = 2,
		LegalNotes = 3,
		FoundationProfileNotes = 4,
	}

	public enum TaxReceiptStep
	{
		Initial = 0,
		Printed = 30,
		Revised = 50,
	}

	public enum TaxReturnIssueType
	{
		QuestionnaireIncomplete = 1,
		MissingDonor = 2,
		MissingCostBasis = 3,
		Reconciliation = 4,
		PendingXouTs = 5,
		UnassignedMiscCredits = 6,
		Difference990Pf = 7,
		PartIiiAdjustments = 8,
		K1Reconciliation = 9,
		TransitionYearReconciliation = 10,
		Other = 11,
		DataChange = 12,
		PurchasedInterestReconciled = 13,
		MissingValuation = 14,
		ExpenditureResponsibility = 15,
		PendingXin = 16,
		Pri = 17,
		MissingStatement = 18,
		QuestionniareAfterPrep = 19,
		K1Outstanding = 20,
		Form4720 = 21,
		AltAssetReview = 22,
		QuestionniareWithIssues = 23,
		NegativeBookBasis = 24,
		Payroll = 25,
		UnamortizedBondPremiums = 26,
		Depreciation = 27,
		NegativeCostBasis = 28,
		TaxReconciliation = 29,
		FirstDraftReview = 30,
		SecondDraftReview = 31,
		ThirdDraftReview = 32,
		InternalReview = 33,
		BegBalanceDiscrepancy = 34,
		CloselyHeldAndOffshoreHedgeFundReview = 35,
		// These are virtual issues
		QuestionnaireOnly = 36,
		OneIssueOnly = 37,
		TwoIssuesOnly = 38,
		ThreeIssuesOnly = 39
	}

	public enum TaxReturnIssuesGroup
	{
		ClientServices = 1,
		FinancialOperations = 2,
		EdShapiro = 3,
		TaxLegal = 4,
		Misc = 5,
	}

	public enum TaxReturnIssueSource
	{
		Auto = 1,
		Manual = 2,
	}

	public enum TaxReturnIssueStatus
	{
		NotDetected = -99,
		NonIssue = -1,
		Issue = 0,
		ManuallyClosed = 99,
	}

	public enum TaxReturnStatus
	{
		NotResponsible = -1,
		NotReady = 0,
		Ready = 70,
		SentToPrep = 80,
		SentWithIssues = 85,
		SentToProcessing = 90,
		SentToIrs = 95,
		Finalized = 99,
	}

	public enum TaxReturnState1
	{
		Default = 0,
		GettingReady = 10,
		Wip = 20,
		InPrepIssues = 30,
		SentToIrs = 95,
		Finalized = 99,
	}

	public enum TaxReturnState2
	{
		Default = 0,
		NotReady = 10,
		Ready = 99,
	}

	public enum TaxReturnState3
	{
		None = -1,
		Default = 0,
		AssignedForPreparation = 5,
		InPrep = 10,
		ReadyForReview = 20,
		AssignedForReview = 25,
		FirstReview = 30,
		SecondReview = 40,
		FinalCorrection = 50,
		FinalReview = 55,
		PendingUpload = 60,
		TaxCenterReview = 70,
	}

	public enum TaxReturnState4
	{
		None = -1,
		Default = 0,
		PendingClientReview = 30,
		ClientsComment = 40,
		//Awaiting8879 = 45,
		//Received8879 = 48,
		//ReadyEFile = 50,
		//Transmitted = 53,
		//FailedEFile = 55,
		//ReadyToPrint = 60,
		//ReadyToMail = 65,
		//SentToClient = 70,
		//SignedMailedByClient = 80,
		//PaperReceived = 85,
		//Assembled = 87,
		//ReadyForFiling = 90,
		IncompletePackage = 100,
		//HybridPackageRcv = 110,
		//ReadyToEfileAssembled = 120,
		//ReadyForEfilePaperFiled = 130,
		//TransmittedReadyForFiling = 140,
		//TransmittedPaperFiled = 150,
		//FailedToEfileForHybrid = 160,
		FormReadyForFiling = 170
	}

	public enum TaxReturnFilingMethod
	{
		None = -1,
		Paper = 1,
		EFile = 2,
		Hybrid = 3,
	}

	public enum TaxReturnComment
	{
		Client = 0,
		Admin = 1,
		Tax = 2,
	}

	public enum TaxCenterState
	{
		NotStarted = 0,
		InReview = 10,
		PrintAndSign = 20,
		Send = 30,
		Done = 99,
	}

	public enum TaxReturnPriority
	{
		None = -1,
		Critical = 1,
		ClientReq = 2,
		NoExt = 3,
		High = 4,
		NoPriority = 5,
		OnlyPriority = 6
	}

	public enum TaxReturnFormState
	{
		None = -1,
		Created = 0,
		PendingReview = 2,
		Received8879 = 4,
		Incomplete = 6,
		Verified = 8,
		ReadyToFile = 10,
		ReadyForCheckRequest = 20,
		CheckEntered = 22,
		WaitingForCheck = 30,
		Rejected = 40,
		Transmitted = 50,
		Assembled = 70,
		Correction = 80,
		Reviewed = 90,
		Filed = 999,
		Done = 1010
	}

	public enum TaxReturnFormMethod
	{
		Paper = 1,
		EFile = 2
	}

	public enum TypePackage
	{
		NineNinetynPF = 1,
		Extension = 2
	}
	public enum DeliveryMethod
	{
		Upload = 1,
		Fax = 2,
		Mail = 3,
		Email = 4,
	}

	public enum TransferMethod
	{
		None = 0,
		Wire = 1,
		Check = 2,
		SnbFileTransfer = 5
	}

	public enum TransferType
	{
		Normal = 0,
		Wire = 1,
		ACH = 2
	}

	public enum EstimatedTaxIssueType
	{
		Reconciliation = 1,
		MissingCostBasis = 2,
		UnknowLastYear = 3,
		OtherIncome = 4,
		MiscCredits = 5,
		PendingXin = 6,
		DataChange = 7,
		Other = 8,
		MissingStatements = 9,
		CapitalGainsReview = 10,
		NotFunded = 11,
		Upload990Pf = 12,
		PartnerContrbDistrib = 13,
		OtherAnnualization = 14,
		AnnuityRecorded = 15
	}

	public enum EstimatedTaxStatus
	{
		NotResponsible = -1,
		NotReady = 0,
		PartiallyReady = 70,
		Ready = 71,
		PartiallyReadyManual = 74,
		ReadyManual = 75,
		InPrep = 77,
		Prepared = 78,
		Reviewed = 80,
		PendingConfirmation = 81,
		UserConfirmation = 82,
		PendingRejection = 83,
		SentToFinance = 85,
		Paid = 99,
	}

	public enum EstimatedTaxIssueStatus
	{
		NotDetected = -99,
		NonIssue = -1,
		Issue = 1,
		ManuallyOpened = 35,
		ManuallyClosed = 99,
	}

	public enum CalcMethod990Pf
	{
		Annualization = 1,
		LastYearActualTaxLiability = 2,
		ClientsTaxAdvisorInstruction = 3,
		UnknownMethod = 4,
	}

	public enum ProcessingOffice
	{
		Fairfield = 1,
		LakeSuccess = 2,
	}

	public enum PartnershipStep
	{
		OutStanding = 10,
		Received = 20,
		Review = 30,
		Hold = 35,
		Processed = 40,
	}

	public enum IrsFileState
	{
		Monitoring = -1,
		Initial = 0,
		Downloading = 10,
		Downloaded = 20,
		Parsing = 30,
		Parsed = 40,
		Importing = 50,
		Imported = 99,
		InitialStatusOk = 0,
		InitialStatusPageNotFound = 1,
		InitialStatusPageChanged = 2,
		InitialStatusIrsFileNotFound = 3,
	}

	public enum IrsFileStatus
	{
		Ok = 0,
		PageNotFound = 1,
		PageChanged = 2,
		IrsFileNotFound = 3
	}

	public enum TaxEmail
	{
		AutoReminder = 1,
		PcaReminder = 2,
	}

	public enum PayeeCompanyType
	{
		Other = 0,
		Corporation = 1,
		Partnership = 2,
		ExemptEntity = 3,
	}

	public enum FoundationAccountStatement
	{
		Mailed = 1,
		Faxed = 2,
		Online = 3,
		EMailStatementsFolder = 4,
		InProcess = 5,
		NotAvailablePerFi = 6,
		PcaToGetFromClient = 7,
		ImplementationsToRequestStmt = 8,
		FinanceToRequest = 9
	}

	public enum BalanceFileType
	{
		PositionFile = 1,
		PaperStatement = 2,
		OperatingAccount = 3,
	}

	public enum BalanceFileStep
	{
		Ignored = -5,
		NotMatched = 0,
		Matched = 1,
		StatementRecieved = 3,
		OnHoldWithIssues = 5,
		ReadyWithCleanUp = 10,
		//Ready = 20, Removed spint 1099 WI 10138
		SecondReview = 50,
		YearEndReview = 70,
		Done = 99
	}

	public enum BalanceFileAction
	{
		None = 0,
		ApproveWithDiff = 1,
		Approve = 2,
		CompleteReview = 3,
		CompleteYEReview = 4,
		BulkClose = 5,
		Ignore = 6,
		UndoIgnore = 7
	}

	public enum AccountType
	{
		Checking = 1,
		Investment = 2,
		ForeignInvestmentAcct = 3,
		HedgefundPartnership = 4,
		Other = 5
	}

	public enum BalanceFileIssueStep
	{
		NotDetected = -99,
		NotIssue = -1,
		Issue = 1,
		ManuallyOpened = 35,
		ManuallyClosed = 99
	}

	public enum BalanceFileIssueType
	{
		CorporateAction = 1,
		PreviousMonthPricing = 2,
		AssetMoving = 3,
		ClientServices = 4,
		Implementations = 5,
		TaxAndLegal = 6,
		Other = 7,
		PendingXIn = 8,
		PendingXOut = 9,
		MissignCostBasis = 10,
		MissignDonor = 11
	}

	public enum FoundationAccountFrequency
	{
		Monthly = 11,
		Quarterly = 13,
		Annually = 19,
		SemiAnnually = 20
	}
	public enum Priority
	{
		None = 0,
		Regular = 1,
		High = 2,
		Critical = 3
	}

	public enum AccountAccess
	{
		FiDataFile = 1,
		BalanceOnly = 5,
		Wtrisc = 6,
		ManualEntryLowActivity = 7,
		ManualEntryClientDeclined = 8,
		ManualEntryNotAvailablePerFi = 9,
		InProcessWithAdvent = 10,
		AdventAcd = 11,
		AdventBaa = 12,
		Other = 99
	}

	public enum DocTheme
	{
		Fsa = 51,
		NoticeFee = 1003,
		LetterOfFeeChanges = 198
	}

	public enum Ten99DefaultBox
	{
		None,
		Box1,
		Box3,
		Box7
	}

	public enum BillStep
	{
		Waived = -10,
		NotReady = 0,
		Ready = 10,
		Sent = 99
	}

	//public enum BillIssueType
	//{
	//	NotReconciled = 1,
	//	ContractReview = 2,
	//	InitQuaterReview = 3,
	//	SignificantChange = 4,
	//	XOUT = 5,
	//	NotFunded = 6,
	//	Other = 7,
	//	SpecialInstruction = 8,
	//	FilingFeesReview = 9,
	//	AssetsFunding = 10
	//}

	public enum BillIssueStep
	{
		NotDetected = -99,
		NotIssue = -1,
		Issue = 1,
		ManuallyOpened = 35,
		ManuallyClosed = 99
	}

	public enum InvoiceStep
	{
		ToBeSent = 0,
		PendingPayment = 30,
		Done = 99,
		Deleted = -1
	}

	public class BridgerRules
	{
		public const string CharityBusiness = "CharityBusiness";
		public const string CharityIndividual = "CharityIndividual";
		public const string ExpenseBusiness = "ExpenseBusiness";
		public const string ExpenseIndividual = "ExpenseIndividual";
	}

	public enum TempIdType
	{
		Payment = 1,
		Grant = 2
	}

	public enum BalanceFileError
	{
		Account = 1,
		WasUploaded = 2,
		UnknownSymbol = 3,
		Other = 5
	}

	public enum SubscriptionLevel
	{
		None = 0,
		Person = 1,
		Foundation = 2,
	}

	public enum HouseholdApplicationType
	{
		None = 0,
		EmergencyAssistance = 1,
		HardshipAssistanceLowInc = 2,
		HardshipAssistanceModInc = 3,
		ScholarshipGrant = 4,
		EmergencyAssistanceMedical = 5,
	}

	public enum HouseholdApplicationStep
	{
		Cancelled = -1,
		UnderReview = 0,
		Approved = 99,
	}

	public enum HouseholdLimitType
	{
		Amount = 1,
		Income = 2,
		NetWorth = 3,
	}
}
