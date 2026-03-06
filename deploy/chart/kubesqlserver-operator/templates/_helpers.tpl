{{/*
Expand the name of the chart.
*/}}
{{- define "kubesqlserver-operator.name" -}}
{{- default .Chart.Name .Values.nameOverride | trunc 63 | trimSuffix "-" }}
{{- end }}

{{/*
Create a default fully qualified app name.
*/}}
{{- define "kubesqlserver-operator.fullname" -}}
{{- if .Values.fullnameOverride }}
{{- .Values.fullnameOverride | trunc 63 | trimSuffix "-" }}
{{- else }}
{{- $name := default .Chart.Name .Values.nameOverride }}
{{- if contains $name .Release.Name }}
{{- .Release.Name | trunc 63 | trimSuffix "-" }}
{{- else }}
{{- printf "%s-%s" .Release.Name $name | trunc 63 | trimSuffix "-" }}
{{- end }}
{{- end }}
{{- end }}

{{/*
Create chart name and version as used by the chart label.
*/}}
{{- define "kubesqlserver-operator.chart" -}}
{{- printf "%s-%s" .Chart.Name .Chart.Version | replace "+" "_" | trunc 63 | trimSuffix "-" }}
{{- end }}

{{/*
Merge global and component-specific labels.
Component-specific labels take precedence over global labels.
Usage: {{ include "kubesqlserver-operator.labels" (dict "global" .Values.global.labels "component" .Values.controller.labels) }}
*/}}
{{- define "kubesqlserver-operator.labels" -}}
{{- $global := .global | default dict }}
{{- $component := .component | default dict }}
{{- $merged := merge $component $global }}
{{- range $key, $value := $merged }}
{{ $key }}: {{ $value | quote }}
{{- end }}
{{- end }}

{{/*
Merge global and component-specific annotations.
Component-specific annotations take precedence over global annotations.
Usage: {{ include "kubesqlserver-operator.annotations" (dict "global" .Values.global.annotations "component" .Values.controller.annotations) }}
*/}}
{{- define "kubesqlserver-operator.annotations" -}}
{{- $global := .global | default dict }}
{{- $component := .component | default dict }}
{{- $merged := merge $component $global }}
{{- range $key, $value := $merged }}
{{ $key }}: {{ $value | quote }}
{{- end }}
{{- end }}

{{/*
Merge global and component-specific pod labels.
Component-specific pod labels take precedence over global pod labels.
Usage: {{ include "kubesqlserver-operator.podLabels" (dict "global" .Values.global.podLabels "component" .Values.controller.podLabels) }}
*/}}
{{- define "kubesqlserver-operator.podLabels" -}}
{{- $global := .global | default dict }}
{{- $component := .component | default dict }}
{{- $merged := merge $component $global }}
{{- range $key, $value := $merged }}
{{ $key }}: {{ $value | quote }}
{{- end }}
{{- end }}

{{/*
Merge global and component-specific pod annotations.
Component-specific pod annotations take precedence over global pod annotations.
Usage: {{ include "kubesqlserver-operator.podAnnotations" (dict "global" .Values.global.podAnnotations "component" .Values.controller.podAnnotations) }}
*/}}
{{- define "kubesqlserver-operator.podAnnotations" -}}
{{- $global := .global | default dict }}
{{- $component := .component | default dict }}
{{- $merged := merge $component $global }}
{{- range $key, $value := $merged }}
{{ $key }}: {{ $value | quote }}
{{- end }}
{{- end }}

{{/*
Merge global and component-specific pod security context.
Component-specific values take precedence over global values.
Usage: {{ include "kubesqlserver-operator.podSecurityContext" (dict "global" .Values.global.podSecurityContext "component" .Values.controller.podSecurityContext) | nindent 8 }}
*/}}
{{- define "kubesqlserver-operator.podSecurityContext" -}}
{{- $global := .global | default dict }}
{{- $component := .component | default dict }}
{{- $merged := mustMergeOverwrite (deepCopy $global) $component }}
{{- if $merged }}
{{- toYaml $merged }}
{{- end }}
{{- end }}

{{/*
Merge global and component-specific container security context.
Component-specific values take precedence over global values.
Usage: {{ include "kubesqlserver-operator.securityContext" (dict "global" .Values.global.securityContext "component" .Values.controller.securityContext) | nindent 12 }}
*/}}
{{- define "kubesqlserver-operator.securityContext" -}}
{{- $global := .global | default dict }}
{{- $component := .component | default dict }}
{{- $merged := mustMergeOverwrite (deepCopy $global) $component }}
{{- if $merged }}
{{- toYaml $merged }}
{{- end }}
{{- end }}

{{/*
Merge global and component-specific node selector.
Component-specific values take precedence over global values.
Usage: {{ include "kubesqlserver-operator.nodeSelector" (dict "global" .Values.global.nodeSelector "component" .Values.controller.nodeSelector) | nindent 8 }}
*/}}
{{- define "kubesqlserver-operator.nodeSelector" -}}
{{- $global := .global | default dict }}
{{- $component := .component | default dict }}
{{- $merged := merge $component $global }}
{{- if $merged }}
{{- toYaml $merged }}
{{- end }}
{{- end }}

{{/*
Merge global and component-specific tolerations.
Component tolerations are appended to global tolerations.
Usage: {{ include "kubesqlserver-operator.tolerations" (dict "global" .Values.global.tolerations "component" .Values.controller.tolerations) | nindent 8 }}
*/}}
{{- define "kubesqlserver-operator.tolerations" -}}
{{- $global := .global | default list }}
{{- $component := .component | default list }}
{{- $merged := concat $global $component | uniq }}
{{- if $merged }}
{{- toYaml $merged }}
{{- end }}
{{- end }}

{{/*
Merge global and component-specific affinity.
Component-specific values take precedence over global values.
Usage: {{ include "kubesqlserver-operator.affinity" (dict "global" .Values.global.affinity "component" .Values.controller.affinity) | nindent 8 }}
*/}}
{{- define "kubesqlserver-operator.affinity" -}}
{{- $global := .global | default dict }}
{{- $component := .component | default dict }}
{{- $merged := merge $component $global }}
{{- if $merged }}
{{- toYaml $merged }}
{{- end }}
{{- end }}

{{/*
Get priority class name with component override.
Component-specific value takes precedence over global value.
Usage: {{ include "kubesqlserver-operator.priorityClassName" (dict "global" .Values.global.priorityClassName "component" .Values.controller.priorityClassName) }}
*/}}
{{- define "kubesqlserver-operator.priorityClassName" -}}
{{- $global := .global | default "" }}
{{- $component := .component | default "" }}
{{- if $component }}
{{- $component }}
{{- else if $global }}
{{- $global }}
{{- end }}
{{- end }}

{{/*
Get image pull policy with component override.
Component-specific value takes precedence over global value.
Usage: {{ include "kubesqlserver-operator.imagePullPolicy" (dict "global" .Values.global.imagePullPolicy "component" .Values.controller.image.pullPolicy) }}
*/}}
{{- define "kubesqlserver-operator.imagePullPolicy" -}}
{{- $global := .global | default "" }}
{{- $component := .component | default "" }}
{{- if $component }}
{{- $component }}
{{- else if $global }}
{{- $global }}
{{- else }}
{{- "IfNotPresent" }}
{{- end }}
{{- end }}

{{/*
Common labels that should be applied to all resources
*/}}
{{- define "kubesqlserver-operator.commonLabels" -}}
helm.sh/chart: {{ include "kubesqlserver-operator.chart" . }}
{{ include "kubesqlserver-operator.selectorLabels" . }}
{{- if .Chart.AppVersion }}
app.kubernetes.io/version: {{ .Chart.AppVersion | quote }}
{{- end }}
app.kubernetes.io/managed-by: {{ .Release.Service }}
{{- end }}

{{/*
Selector labels
*/}}
{{- define "kubesqlserver-operator.selectorLabels" -}}
app.kubernetes.io/name: {{ include "kubesqlserver-operator.name" . }}
app.kubernetes.io/instance: {{ .Release.Name }}
{{- end }}

{{/*
Service account name
*/}}
{{- define "kubesqlserver-operator.serviceAccountName" -}}
{{- if .Values.controller.serviceAccount.create }}
{{- default (printf "%s-operator-sa" .Release.Name) .Values.controller.serviceAccount.name }}
{{- else }}
{{- default "default" .Values.controller.serviceAccount.name }}
{{- end }}
{{- end }}
