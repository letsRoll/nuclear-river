Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
#Requires –Version 3.0
#------------------------------

$topicProperties = @{
	'Properties' = @{
		'EnableBatchedOperations' = $true
		'SupportOrdering' = $true
		'RequiresDuplicateDetection' = $true
	}
}

$subscriptionProperties = @{
	'Properties' = @{
		'EnableBatchedOperations' = $true
		'MaxDeliveryCount' = 0x7fffffff
	}
}

function Get-ServiceBusMetadata ($Context) {

	$metadata = @{}
	$metadata += Get-TopicsMetadata $Context

	$metadata['UseCaseRoute'] = $Context.UseCaseRoute

	return @{ 'ServiceBus' = $metadata}
}

function Get-TopicsMetadata ($Context) {

	$metadata = @{}

	switch ($Context.EntryPoint) {

		'CustomerIntelligence.Replication.Host' {

			switch ($Context.UseCaseRoute){
				{ $_ -eq 'ERM' -or $_ -eq $null } {
					$ermEventsFlowTopic = @{
						'ErmEventsFlowTopic' = @{
							'Name' = 'topic.performedoperations'
							'ConnectionStringName' = 'ServiceBus'
						} + $topicProperties
					}
					$ermEventsFlowSubscription = @{
						'ErmEventsFlowSubscription' = @{
							'TopicName' = 'topic.performedoperations'
							'Name' = '9F2C5A2A-924C-485A-9790-9066631DB307'
							'ConnectionStringName' = 'ServiceBus'
						} + $subscriptionProperties
					}
					$deleteTopic = @{
						'DeleteConvertUseCasesTopic-ERMProduction' = @{
							'Name' = "topic.performedoperations.production.$($Context.Country).import".ToLowerInvariant()
							'ConnectionStringName' = 'ServiceBus'
						}
					}
					$deleteSubscription = @{}
				}

				'ERMProduction' {
					$ermEventsFlowTopic = @{
						'ErmEventsFlowTopic' = @{
							'Name' = "topic.performedoperations.production.$($Context.Country).import".ToLowerInvariant()
							'ConnectionStringName' = 'ServiceBus'
						} + $topicProperties
					}
					$ermEventsFlowSubscription = @{
						'ErmEventsFlowSubscription' = @{
							'TopicName' = "topic.performedoperations.production.$($Context.Country).import".ToLowerInvariant()
							'Name' = '9F2C5A2A-924C-485A-9790-9066631DB307'
							'ConnectionStringName' = 'ServiceBus'
						} + $subscriptionProperties
					}
					$deleteTopic = @{}
					$deleteSubscription = @{
						'DeleteErmEventsFlowSubscription' = @{
							'TopicName' = 'topic.performedoperations'
							'Name' = '9F2C5A2A-924C-485A-9790-9066631DB307'
							'ConnectionStringName' = 'ServiceBus'
						}
					}
				}
			}

			$metadata = @{
				'CreateTopics' = @{
					'CommonEventsFlowTopic' = @{
						'Name' = 'topic.river.common'
						'ConnectionStringName' = 'ServiceBus'
					} + $topicProperties

					'StatisticsEventsFlowTopic' = @{
						'Name' = 'topic.river.statistics'
						'ConnectionStringName' = 'ServiceBus'
					} + $topicProperties

				} + $ermEventsFlowTopic

				'DeleteTopics' = $deleteTopic

				'CreateSubscriptions' = @{
					'CommonEventsFlowSubscription' = @{
						'TopicName' = 'topic.river.common'
						'Name' = '96F17B1A-4CC8-40CC-9A92-16D87733C39F'
						'ConnectionStringName' = 'ServiceBus'
					} + $subscriptionProperties

					'StatisticsEventsFlowSubscription' = @{
						'TopicName' = 'topic.river.statistics'
						'Name' = 'EED0A445-4A53-4D49-89F5-01DD440C85C8'
						'ConnectionStringName' = 'ServiceBus'
					} + $subscriptionProperties
				} + $ermEventsFlowSubscription

				'DeleteSubscriptions' = $deleteSubscription
			} 
		}

		# нужен чтобы удалить subscription с production, т.к. только он знает нужный connectionString
		'ConvertUseCasesService-Production' {
			switch ($Context.UseCaseRoute){
				{ $_ -eq 'ERM' -or $_ -eq $null } {
					$metadata = @{
						'DeleteSubscriptions' = @{
							'DeleteConvertUseCasesSubscription-ERMProduction' = @{
								'TopicName' = 'topic.performedoperations.export'
								'Name' = $Context.EnvironmentName.ToLowerInvariant()
								'ConnectionStringName' = 'Source'
							}
						}
					}
				}
			}
		}

		'ConvertUseCasesService' {
			switch ($Context.UseCaseRoute){
				'ERMProduction' {
					$metadata = @{
						'CreateTopics' = @{
							'SourceTopic' = @{
								'Name' = 'topic.performedoperations.export'
								'ConnectionStringName' = 'Source'
							} + $topicProperties
							'DestTopic' = @{
								'Name' = "topic.performedoperations.production.$($Context.Country).import".ToLowerInvariant()
								'ConnectionStringName' = 'Dest'
							} + $topicProperties
						}
						'CreateSubscriptions' = @{
							'SourceSubscription' = @{
								'TopicName' = 'topic.performedoperations.export'
								'Name' = $Context.EnvironmentName.ToLowerInvariant()
								'ConnectionStringName' = 'Source'
							} + $subscriptionProperties

							'DestSubscription' = @{
								'TopicName'  = "topic.performedoperations.production.$($Context.Country).import".ToLowerInvariant()
								'Name' = '9F2C5A2A-924C-485A-9790-9066631DB307'
								'ConnectionStringName' = 'Dest'
							} + $subscriptionProperties
						}
					}
				}
			}
		}
	}

	return $metadata
}

Export-ModuleMember -Function Get-ServiceBusMetadata