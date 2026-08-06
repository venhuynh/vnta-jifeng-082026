# 00 - Feature refactor manifest

## Cách dùng

Thay toàn bộ placeholder {{...}} trong khối sau. Đây là đầu vào duy nhất cần điền bằng tay trước khi chạy các prompt còn lại. Nếu chưa biết một giá trị, ghi DISCOVER thay vì đoán.

    repository_root: Vnta-Blazor-2026
    solution_path: Vnta-Blazor-2026/src/Vnta.HRM2026/Vnta.Hrm.slnx
    analysis_artifact_root: {{ANALYSIS_ARTIFACT_ROOT_OR_CHAT_ONLY}}

    feature:
      display_name: {{TEN_MAN_HINH_NGHIEP_VU}}
      group: {{NHOM_NGHIEP_VU}}
      context_key: {{CONTEXT_KEY_PASCAL_CASE}}
      ui_entry: {{RAZOR_FILE_OR_FEATURE_FOLDER_OR_ROUTE}}
      expected_behavior: {{MO_TA_NGHIEP_VU_NGAN_GON}}
      primary_use_cases: {{READ_SEARCH_EXPORT_MUTATION_LOCK_OR_OTHER}}

    reference_standard:
      display_name: Phụ cấp chuyên cần
      client_root: Vnta-Blazor-2026/src/Vnta.HRM2026/Vnta.Hrm.Web.Client/Components/PhuCap/PhuCapChuyenCan
      revision: {{HEAD_OR_COMMIT_THAM_CHIEU}}
      use_as: architecture-boundaries-and-quality-gates-only

    scope:
      mode: {{AUDIT_ONLY_OR_STRUCTURAL_REFACTOR_OR_APPROVED_BEHAVIOR_FIX}}
      allowed_changes: {{PHAM_VI_DUOC_PHEP}}
      out_of_scope: {{PHAM_VI_CAM}}
      route_payload_compatibility: {{PRESERVE_OR_APPROVED_CHANGE}}
      authorization_compatibility: {{PRESERVE_OR_APPROVED_CHANGE}}
      schema_migration: {{FORBIDDEN_OR_APPROVED}}
      business_rule_change: {{FORBIDDEN_OR_APPROVED_WITH_DESCRIPTION}}
      data_ownership_change: {{FORBIDDEN_OR_APPROVED_WITH_DESCRIPTION}}
      documentation: {{CHAT_ONLY_OR_UPDATE_RELEVANT_DOCS}}
      commit_strategy: {{FINAL_WORK_ITEM_OR_EACH_INDEPENDENT_PHASE}}

    branch:
      required_before_refactor: true
      base: {{APPROVED_BASE_BRANCH}}
      name: {{NEW_DEDICATED_REFACTOR_BRANCH_NAME}}
      base_commit: {{DISCOVER_AND_RECORD_BEFORE_BRANCH_CREATION}}

    known_constraints:
      existing_public_consumers: {{DISCOVER_OR_LIST}}
      legacy_contracts_to_preserve: {{NONE_OR_LIST}}
      protected_existing_changes: {{NONE_OR_GIT_PATHS_OR_DESCRIPTION}}
      security_tenant_actor_constraints: {{DISCOVER_OR_LIST}}
      performance_volume_sla: {{DISCOVER_OR_LIST}}

    source_commenting:
      language: {{TIENG_VIET_CO_DAU_OR_REPOSITORY_STANDARD}}
      scope: {{CHANGED_PUBLIC_BOUNDARIES_AND_NON_OBVIOUS_LOGIC}}
      xml_documentation: {{REQUIRED_FOR_EXPOSED_CONTRACTS}}
      detail_level: {{EXPLAIN_INTENT_INVARIANT_SIDE_EFFECT_RATIONALE_NOT_SYNTAX}}
      generated_vendor_migration_files: {{DO_NOT_EDIT_UNLESS_APPROVED}}
      traceability: {{COMMENT_MAP_REQUIRED_OR_NA}}

    verification:
      client_build: dotnet build Vnta-Blazor-2026/src/Vnta.HRM2026/Vnta.Hrm.Web.Client/Vnta.Hrm.Web.Client.csproj --no-restore
      web_build: {{COMMAND_OR_NA}}
      test_commands: {{ONE_OR_MORE_COMMANDS_OR_DISCOVER}}
      integration_prerequisites: {{DATABASE_CONTAINER_SECRET_OR_NA}}

## Prompt khởi tạo

Hãy dùng Feature Refactor Manifest dưới đây làm nguồn đầu vào duy nhất cho toàn bộ công việc. Trước hết đọc AGENTS.md áp dụng cho {{repository_root}}, chạy git status --short --branch, và xác nhận manifest không mâu thuẫn với repository hiện tại.

Chưa được refactor ở bước này. Hãy:

- xác nhận mode, phạm vi được phép và các boundary không được tự đổi;
- thay giá trị DISCOVER bằng danh sách discovery cần thực hiện ở bước 01, không bịa tên file/route/entity;
- nêu các input tối thiểu còn thiếu mà source không thể tự phát hiện;
- xác định artifact nào cần được giữ lại cho các bước sau: source map, invariant matrix, compatibility ledger và verification baseline;
- xác nhận Branch Gate: audit/plan chưa được tạo nhánh, nhưng trước lần sửa source/config phải tạo branch.name mới từ branch.base theo README; nếu branch input chưa hợp lệ, trạng thái phải là NEEDS_USER_DECISION;
- nhắc lại rằng Phụ cấp chuyên cần chỉ là chuẩn kiến trúc/quality gate, không phải implementation để copy nguyên xi.

Kết thúc bằng một bản Manifest đã chuẩn hóa, một danh sách DISCOVER và trạng thái GO hoặc NEEDS_USER_DECISION. Không sửa file, config, migration hoặc git history.
